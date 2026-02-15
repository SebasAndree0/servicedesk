// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Controllers\AdminSlaController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Domain.Entities;
using ServiceDesk.Api.Domain.Enums;
using ServiceDesk.Api.Infrastructure.Db;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/sla")]
public class AdminSlaController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminSlaController(AppDbContext db) => _db = db;

    // GET /api/admin/sla
    [HttpGet]
    public async Task<ActionResult<object>> List()
    {
        var items = await _db.SlaRules.AsNoTracking()
            .OrderBy(x => x.Priority)
            .Select(x => new
            {
                x.Id,
                Priority = x.Priority.ToString(),
                x.Hours,
                x.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(new { items });
    }

    // PUT /api/admin/sla/P1   body: { "hours": 2 }
    [HttpPut("{priority}")]
    public async Task<ActionResult<object>> Upsert([FromRoute] string priority, [FromBody] UpsertSlaRequest req)
    {
        if (!Enum.TryParse<TicketPriority>(priority, ignoreCase: true, out var p))
            return BadRequest(new { error = "Priority inválida (usa P1, P2, P3)" });

        if (req is null)
            return BadRequest(new { error = "Body requerido: { \"hours\": 24 }" });

        if (req.Hours < 1 || req.Hours > 720)
            return BadRequest(new { error = "Hours debe estar entre 1 y 720" });

        var rule = await _db.SlaRules.FirstOrDefaultAsync(x => x.Priority == p);

        if (rule is null)
        {
            rule = new SlaRule
            {
                Priority = p,
                Hours = req.Hours,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _db.SlaRules.Add(rule);
        }
        else
        {
            rule.Hours = req.Hours;
            rule.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            rule.Id,
            Priority = rule.Priority.ToString(),
            rule.Hours,
            rule.UpdatedAtUtc
        });
    }
}

public record UpsertSlaRequest(int Hours);
