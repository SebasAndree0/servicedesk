using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization; // ✅ NEW
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ServiceDesk.Api.Domain.Entities;
using ServiceDesk.Api.Domain.Options;
using ServiceDesk.Api.Infrastructure.Db;
using ServiceDesk.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// =======================
// JWT Debug (solo DEV)
// =======================
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

// =======================
// Config JWT desde appsettings.json
// =======================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtKey = jwtSection["Key"];

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience) ||
    string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Falta configuración JWT. Revisa appsettings.json: Jwt:Issuer, Jwt:Audience, Jwt:Key");
}

// =======================
// Services
// =======================
// ✅ FIX: Enums como STRING en toda la API ("Open", "Closed", "P2", etc.)
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // (opcional) si quieres más tolerancia: o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ✅ Service para TicketsController
builder.Services.AddScoped<TicketActivityWriter>();

// =======================
// CORS (PRO: prepara deploy / web separado)
// =======================
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("web", p =>
    {
        p.WithOrigins(
                "http://localhost:5252", // tu MVC
                "https://localhost:5252"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// =======================
// Swagger + JWT Auth (PRO)
// =======================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ServiceDesk API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Pega: Bearer {tu_token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// =======================
// DB + Identity
// =======================
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // ✅ Password simple (DEV)
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// =======================
// Auth JWT
// =======================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // ✅ Solo DEV sin https metadata
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            // ✅ CLAVE: usamos ClaimTypes.Role (estándar)
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        // ✅ Mapeo automático: si tu token trae "role" o "roles", lo convertimos a ClaimTypes.Role
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("JWT ❌ Auth Failed");
                Console.WriteLine(ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine($"JWT ⚠️ Challenge: {ctx.Error} | {ctx.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var identity = ctx.Principal?.Identity as ClaimsIdentity;
                if (identity != null)
                {
                    // Mapear "role" -> ClaimTypes.Role
                    var roleClaims = identity.FindAll("role").Select(c => c.Value).ToList();
                    foreach (var r in roleClaims)
                    {
                        if (!identity.HasClaim(ClaimTypes.Role, r))
                            identity.AddClaim(new Claim(ClaimTypes.Role, r));
                    }

                    // Mapear "roles" -> ClaimTypes.Role
                    var rolesClaims = identity.FindAll("roles").Select(c => c.Value).ToList();
                    foreach (var r in rolesClaims)
                    {
                        if (!identity.HasClaim(ClaimTypes.Role, r))
                            identity.AddClaim(new Claim(ClaimTypes.Role, r));
                    }
                }

                var name = ctx.Principal?.Identity?.Name ?? "(sin Name)";
                var roles = ctx.Principal?.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Distinct()
                    .ToList() ?? new List<string>();

                Console.WriteLine($"JWT ✅ OK User: {name}");
                Console.WriteLine($"JWT Roles: {(roles.Count == 0 ? "(none)" : string.Join(", ", roles))}");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// =======================
// App
// =======================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

// ✅ CORS antes de Auth (importante)
app.UseCors("web");

// 🔥 Orden correcto
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
