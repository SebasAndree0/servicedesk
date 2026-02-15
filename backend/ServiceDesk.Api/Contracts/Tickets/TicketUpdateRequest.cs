using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Contracts.Tickets;

public class TicketUpdateRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    // "P1" / "P2" / "P3" desde la web
    [Required]
    [RegularExpression("^P[1-3]$", ErrorMessage = "Priority debe ser P1, P2 o P3")]
    public string Priority { get; set; } = "P2";

    [Required]
    [StringLength(100)]
    public string By { get; set; } = "";

    // ✅ NUEVO: asignación (puede ser null / vacío)
    [StringLength(120)]
    public string? AssignedTo { get; set; }
}
