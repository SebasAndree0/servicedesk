using ServiceDesk.Api.Domain.Enums;

namespace ServiceDesk.Api.Domain.Entities;

public class SlaRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TicketPriority Priority { get; set; } = TicketPriority.P3;

    public int Hours { get; set; } = 72;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
