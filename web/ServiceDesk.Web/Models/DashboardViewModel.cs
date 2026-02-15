using ServiceDesk.Web.Models.Tickets;

namespace ServiceDesk.Web.Models;

public class DashboardViewModel
{
    public int TotalTickets { get; set; }
    public int Open { get; set; }
    public int InProgress { get; set; }
    public int Closed { get; set; }

    public List<TicketResponse> Latest { get; set; } = new();
}
