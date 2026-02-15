// C:\ProyectoASPNET\servicedesk\web\ServiceDesk.Web\Controllers\HomeController.cs

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Web.Models;
using ServiceDesk.Web.Models.Tickets;

namespace ServiceDesk.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _http;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory http)
    {
        _logger = logger;
        _http = http;
    }

    public async Task<IActionResult> Index()
    {
        var client = _http.CreateClient("api");

        // Últimos 5 tickets para la tabla del dashboard
        var res = await client.GetAsync("api/tickets?page=1&pageSize=5");

        if (!res.IsSuccessStatusCode)
        {
            ViewBag.Error = "No pude cargar el dashboard (API no respondió).";
            return View(new DashboardViewModel());
        }

        var data = await res.Content.ReadFromJsonAsync<PagedTicketsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        var items = data?.Items ?? new List<TicketResponse>();

        // ✅ Ahora Status viene como STRING: "Open" | "InProgress" | "Closed"
        var vm = new DashboardViewModel
        {
            TotalTickets = data?.Total ?? 0,
            Open = items.Count(x => x.Status == "Open"),
            InProgress = items.Count(x => x.Status == "InProgress"),
            Closed = items.Count(x => x.Status == "Closed"),
            Latest = items
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
