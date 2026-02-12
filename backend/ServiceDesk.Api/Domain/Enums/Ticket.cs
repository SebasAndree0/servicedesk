using ServiceDesk.Api.Domain.Enums;

namespace ServiceDesk.Api.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.P3;

    // “Empresa”: multi-user / asignación (por ahora string simple)
    public string CreatedBy { get; set; } = "anonymous";
    public string? AssignedTo { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<TicketEvent> Events { get; set; } = new();
}
