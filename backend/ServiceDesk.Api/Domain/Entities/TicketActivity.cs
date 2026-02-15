namespace ServiceDesk.Api.Domain.Entities;

public class TicketActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }

    // ✅ Relación con Ticket (misma carpeta/namespace)
    public Ticket Ticket { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string By { get; set; } = "";
    public string Action { get; set; } = "";
    public string Message { get; set; } = "";
}
