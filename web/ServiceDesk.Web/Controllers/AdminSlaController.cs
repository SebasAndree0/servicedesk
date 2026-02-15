using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Web.Models.Admin;

namespace ServiceDesk.Web.Controllers;

public class AdminSlaController : Controller
{
    private readonly IHttpClientFactory _http;

    public AdminSlaController(IHttpClientFactory http)
    {
        _http = http;
    }

    // GET: /AdminSla
    public async Task<IActionResult> Index()
    {
        var client = _http.CreateClient("api");

        var res = await client.GetAsync("api/admin/sla");
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            ViewBag.Error = $"No pude cargar SLA. Status: {(int)res.StatusCode}. Body: {body}";
            return View(new SlaRulesListResponse(new List<SlaRuleDto>()));
        }

        var data = await res.Content.ReadFromJsonAsync<SlaRulesListResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return View(data ?? new SlaRulesListResponse(new List<SlaRuleDto>()));
    }

    // POST: /AdminSla/Save
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string priority, int hours)
    {
        if (string.IsNullOrWhiteSpace(priority))
        {
            TempData["Error"] = "Priority es obligatoria.";
            return RedirectToAction("Index");
        }

        if (hours < 1 || hours > 720)
        {
            TempData["Error"] = "Hours debe estar entre 1 y 720.";
            return RedirectToAction("Index");
        }

        var client = _http.CreateClient("api");

        // API: PUT /api/admin/sla/{priority}
        var res = await client.PutAsJsonAsync(
            $"api/admin/sla/{Uri.EscapeDataString(priority)}",
            new UpsertSlaRequest(hours)
        );

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude guardar SLA {priority}. Status: {(int)res.StatusCode}. Body: {body}";
            return RedirectToAction("Index");
        }

        TempData["Success"] = $"✅ SLA {priority} actualizado a {hours}h.";
        return RedirectToAction("Index");
    }
}
