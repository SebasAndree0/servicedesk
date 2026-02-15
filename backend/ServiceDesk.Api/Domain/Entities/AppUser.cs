// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Domain\Entities\AppUser.cs

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceDesk.Api.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    // Nombre visible en UI
    public string DisplayName { get; set; } = "";

    // Estado activo del usuario
    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // ✅ Alias SOLO para código (NO se mapea a DB)
    // Identity usa la columna real "UserName"
    [NotMapped]
    public string Username
    {
        get => UserName ?? "";
        set => UserName = value;
    }

    // Opcional
    public string? FullName { get; set; }
}
