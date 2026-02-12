namespace ServiceDesk.Api.Domain.Entities;

public class TicketEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    // ejemplo: "Created", "StatusChanged", "CommentAdded", "Assigned"
    public string Type { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string By { get; set; } = "system";
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
}
