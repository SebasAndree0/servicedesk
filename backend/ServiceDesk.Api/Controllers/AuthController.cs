using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceDesk.Api.Domain.Entities;
using ServiceDesk.Api.Domain.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtOptions _jwt;

    public AuthController(UserManager<AppUser> userManager, IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _jwt = jwtOptions.Value;
    }

    public record LoginRequest(string UsernameOrEmail, string Password);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UsernameOrEmail) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "usernameOrEmail y password son requeridos" });

        var input = req.UsernameOrEmail.Trim();

        AppUser? user =
            await _userManager.FindByNameAsync(input)
            ?? await _userManager.FindByEmailAsync(input);

        if (user == null)
            return Unauthorized(new { error = "Credenciales inválidas" });

        if (!user.IsActive)
            return Unauthorized(new { error = "Usuario inactivo" });

        var ok = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!ok)
            return Unauthorized(new { error = "Credenciales inválidas" });

        var roles = await _userManager.GetRolesAsync(user);

        var (token, expiresAtUtc) = GenerateJwt(user, roles);

        return Ok(new
        {
            accessToken = token,
            expiresAtUtc,
            user = new
            {
                id = user.Id,
                username = user.UserName,
                email = user.Email,
                displayName = user.DisplayName,
                roles = roles.ToArray()
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        // ✅ En tu token se incluye sub + NameIdentifier, así que con esto basta.
        var userId =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "No sub/NameIdentifier claim" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized(new { error = "User not found" });

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            id = user.Id,
            username = user.UserName,
            email = user.Email,
            displayName = user.DisplayName,
            roles = roles.ToArray()
        });
    }

    private (string token, DateTime expiresAtUtc) GenerateJwt(AppUser user, IList<string> roles)
    {
        if (string.IsNullOrWhiteSpace(_jwt.Key))
            throw new Exception("Jwt:Key no está configurado.");
        if (string.IsNullOrWhiteSpace(_jwt.Issuer))
            throw new Exception("Jwt:Issuer no está configurado.");
        if (string.IsNullOrWhiteSpace(_jwt.Audience))
            throw new Exception("Jwt:Audience no está configurado.");

        var keyBytes = Encoding.UTF8.GetBytes(_jwt.Key.Trim());
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var username = user.UserName ?? user.Email ?? "";
        var email = user.Email ?? "";
        var displayName = string.IsNullOrWhiteSpace(user.DisplayName)
            ? (string.IsNullOrWhiteSpace(email) ? username : email)
            : user.DisplayName;

        var claims = new List<Claim>
        {
            // ✅ estándar JWT
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),

            // ✅ para “CreatedBy automático”
            new("username", username),
            new("display_name", displayName),
            new("email", email),

            // ✅ compatibilidad ASP.NET
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, username),
        };

        if (!string.IsNullOrWhiteSpace(email))
            claims.Add(new Claim(ClaimTypes.Email, email));

        // ✅ ROLES: emitimos en ambos formatos:
        // - "role" (para tu RoleClaimType = "role")
        // - ClaimTypes.Role (para compatibilidad con otras cosas)
        foreach (var r in roles)
        {
            claims.Add(new Claim("role", r));
            claims.Add(new Claim(ClaimTypes.Role, r));
        }

        var now = DateTime.UtcNow;
        var minutes = _jwt.ExpiresMinutes <= 0 ? 120 : _jwt.ExpiresMinutes;
        var expiresAt = now.AddMinutes(minutes);

        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer.Trim(),
            audience: _jwt.Audience.Trim(),
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expiresAt);
    }
}
