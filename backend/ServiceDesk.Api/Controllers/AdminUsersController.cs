// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Controllers\AdminUsersController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Domain.Entities;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AdminUsersController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: api/admin/users
    [HttpGet]
    public async Task<ActionResult<object>> List(CancellationToken ct)
    {
        var items = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.Id,
                Username = u.UserName,      // Identity usa UserName
                u.DisplayName,
                u.Email,
                u.IsActive,
                u.CreatedAtUtc,
                u.UpdatedAtUtc
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    // POST: api/admin/users
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateUserRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username))
            return BadRequest(new { error = "Username is required" });

        if (string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { error = "DisplayName is required" });

        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Password is required" });

        var username = req.Username.Trim().ToLowerInvariant();

        // Username único
        var existsByUsername = await _userManager.FindByNameAsync(username);
        if (existsByUsername != null)
            return Conflict(new { error = "Username already exists" });

        // Email único (si mandaron email)
        if (!string.IsNullOrWhiteSpace(req.Email))
        {
            var existsByEmail = await _userManager.FindByEmailAsync(req.Email.Trim());
            if (existsByEmail != null)
                return Conflict(new { error = "Email already exists" });
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            DisplayName = req.DisplayName.Trim(),
            IsActive = req.IsActive,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        // Rol por defecto (si existe)
        await _userManager.AddToRoleAsync(user, "User");

        return Ok(new { user.Id });
    }

    // PUT: api/admin/users/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { error = "DisplayName is required" });

        // Cambiar username (opcional)
        if (!string.IsNullOrWhiteSpace(req.Username))
        {
            var newUsername = req.Username.Trim().ToLowerInvariant();

            if (!string.Equals(user.UserName, newUsername, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _userManager.FindByNameAsync(newUsername);
                if (exists != null)
                    return Conflict(new { error = "Username already exists" });

                user.UserName = newUsername;
            }
        }

        // Cambiar email (opcional)
        if (!string.IsNullOrWhiteSpace(req.Email))
        {
            var newEmail = req.Email.Trim();

            if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                var existsEmail = await _userManager.FindByEmailAsync(newEmail);
                if (existsEmail != null)
                    return Conflict(new { error = "Email already exists" });

                user.Email = newEmail;
            }
        }

        user.DisplayName = req.DisplayName.Trim();
        user.IsActive = req.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        // Cambiar password (opcional)
        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            // Para cambiar password de admin sin saber la actual:
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passResult = await _userManager.ResetPasswordAsync(user, resetToken, req.NewPassword);

            if (!passResult.Succeeded)
            {
                var errors = string.Join(" | ", passResult.Errors.Select(e => e.Description));
                return BadRequest(new { error = errors });
            }
        }

        return Ok();
    }

    // DELETE: api/admin/users/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        return Ok();
    }
}

// Requests (compatibles con tu estilo, pero adaptados a Identity)
public record CreateUserRequest(
    string Username,
    string DisplayName,
    string Password,
    string? Email = null,
    bool IsActive = true
);

public record UpdateUserRequest(
    string DisplayName,
    bool IsActive = true,
    string? Username = null,
    string? Email = null,
    string? NewPassword = null
);
