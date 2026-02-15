// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Domain\Entities\Ticket.cs

using ServiceDesk.Api.Domain.Enums;

namespace ServiceDesk.Api.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.P3;

    // ✅ CATÁLOGO
    public TicketCategory Category { get; set; } = TicketCategory.Software;
    public TicketType Type { get; set; } = TicketType.Incidencia;

    // “Empresa”: multi-user / asignación
    public string CreatedBy { get; set; } = "anonymous";
    public string? AssignedTo { get; set; }

    // ✅ SLA (horas objetivo según prioridad)
    public int SlaHours { get; set; } = 24;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // ============================================================
    // ✅ SOFT DELETE + TRAZABILIDAD (historial de borrados)
    // ============================================================
    // No se elimina físicamente de la DB; solo se marca como borrado.
    public bool IsDeleted { get; set; } = false;

    // Cuándo se borró
    public DateTime? DeletedAtUtc { get; set; }

    // Quién lo borró
    public string? DeletedBy { get; set; }

    // Motivo obligatorio del borrado
    public string? DeleteReason { get; set; }

    // ============================================================

    public List<TicketEvent> Events { get; set; } = new();

    // ✅ Activity log (historial de cambios/acciones)
    public List<TicketActivity> Activities { get; set; } = new();
}
