namespace ServiceDesk.Web.Models.Tickets;

public class TicketUpdateRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }

    // "P1/P2/P3"
    public string Priority { get; set; } = "P2";

    // quién hace el cambio
    public string By { get; set; } = "sebas";

    // ✅ NUEVO: a quién se asigna
    public string? AssignedTo { get; set; }
}
