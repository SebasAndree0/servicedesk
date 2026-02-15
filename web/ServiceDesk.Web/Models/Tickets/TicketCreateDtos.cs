// C:\ProyectoASPNET\servicedesk\web\ServiceDesk.Web\Models\Tickets\TicketCreateDtos.cs

namespace ServiceDesk.Web.Models.Tickets;

public class TicketCreateRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }

    // "P1/P2/P3"
    public string Priority { get; set; } = "P2";

    // Deben coincidir con enum del API: Hardware | Software | Redes
    public string Category { get; set; } = "Software";

    // Deben coincidir con enum del API: Incidencia | Solicitud
    public string Type { get; set; } = "Incidencia";

    // ✅ PRO: NO hardcodear "sebas". Se setea server-side desde Identity.
    public string CreatedBy { get; set; } = "";

    public string? AssignedTo { get; set; }
}

public record TicketCreateResponse(Guid Id);
