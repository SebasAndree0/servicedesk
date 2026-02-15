using System;
using ServiceDesk.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ✅ MVC + filtro global (candado)
builder.Services.AddControllersWithViews(opt =>
{
    opt.Filters.Add<RequireLoginAttribute>();
});

// ✅ Necesario para usar Session en handlers/filters
builder.Services.AddHttpContextAccessor();

// ✅ Session
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromHours(8);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
});

// ✅ Handler que agrega Authorization: Bearer {token}
builder.Services.AddTransient<ApiAuthHandler>();

// ✅ HttpClient para consumir la API (con handler)
builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri("http://localhost:5030/");
})
.AddHttpMessageHandler<ApiAuthHandler>();

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// dev: lo dejaste deshabilitado
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// ✅ importante: Session antes de MVC
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
