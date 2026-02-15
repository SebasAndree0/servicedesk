using System.ComponentModel.DataAnnotations;
using ServiceDesk.Api.Domain.Enums;

namespace ServiceDesk.Api.Contracts.Tickets;

public class TicketCreateRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required]
    public TicketPriority Priority { get; set; } = TicketPriority.P3;

    // ✅ CATÁLOGO
    public TicketCategory Category { get; set; } = TicketCategory.Software;
    public TicketType Type { get; set; } = TicketType.Incidencia;

    // ✅ SLA
    [Range(1, 720)] // 1h a 30 días
    public int SlaHours { get; set; } = 24;

    [Required]
    [StringLength(120)]
    public string CreatedBy { get; set; } = "";

    // Asignación opcional
    [StringLength(120)]
    public string? AssignedTo { get; set; }
}
