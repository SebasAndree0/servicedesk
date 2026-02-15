// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Controllers\TicketsController.cs

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Contracts.Tickets;
using ServiceDesk.Api.Domain.Entities;
using ServiceDesk.Api.Domain.Enums;
using ServiceDesk.Api.Infrastructure.Db;
using ServiceDesk.Api.Services;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Route("api/v1/tickets")] // ✅ alias para que el Web no tire 404 si usa /api/v1
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TicketActivityWriter _activity;

    public TicketsController(AppDbContext db, TicketActivityWriter activity)
    {
        _db = db;
        _activity = activity;
    }

    // ============================================================
    // ✅ Helpers / DTOs locales (no rompen Contracts)
    // ============================================================

    public record TicketDeleteRequest(string By, string Reason);

    public record TicketEvidenceDeleteRequest(string By, string Reason);

    private static DateTime? ParseDateOnlyAsUtcStart(string? yyyyMmDd)
    {
        if (string.IsNullOrWhiteSpace(yyyyMmDd)) return null;
        if (!DateTime.TryParse(yyyyMmDd.Trim(), out var dt)) return null;

        // Si viene "2026-02-15" lo dejamos como 00:00 UTC
        // (ideal: UI manda yyyy-MM-dd y listo)
        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
    }

    private static DateTime? ParseDateOnlyAsUtcEndInclusive(string? yyyyMmDd)
    {
        if (string.IsNullOrWhiteSpace(yyyyMmDd)) return null;
        if (!DateTime.TryParse(yyyyMmDd.Trim(), out var dt)) return null;

        // Fin del día inclusive: 23:59:59.999...
        var end = dt.Date.AddDays(1).AddTicks(-1);
        return DateTime.SpecifyKind(end, DateTimeKind.Utc);
    }

    // ✅ SLA desde DB (Admin configurable) con fallback
    private async Task<int> SlaFromPriorityAsync(TicketPriority p)
    {
        var rule = await _db.SlaRules.AsNoTracking().FirstOrDefaultAsync(x => x.Priority == p);
        if (rule is not null) return rule.Hours;

        return p switch
        {
            TicketPriority.P1 => 4,
            TicketPriority.P2 => 24,
            _ => 72
        };
    }

    // ============================================================
    // ✅ LIST PRO (filtros avanzados + sort + paginación)
    // ============================================================
    // GET /api/tickets?page=1&pageSize=20
    // &status=Open&priority=P2&category=Software&type=Incidencia&search=texto
    // &createdBy=sebas&assignedTo=soporte
    // &createdFrom=2026-02-01&createdTo=2026-02-15
    // &updatedFrom=2026-02-01&updatedTo=2026-02-15
    // &sortBy=updatedAt&sortDir=desc
    [HttpGet]
    public async Task<ActionResult<object>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] TicketCategory? category = null,
        [FromQuery] TicketType? type = null,
        [FromQuery] string? search = null,

        // ✅ nuevos filtros
        [FromQuery] string? createdBy = null,
        [FromQuery] string? assignedTo = null,
        [FromQuery] string? createdFrom = null,
        [FromQuery] string? createdTo = null,
        [FromQuery] string? updatedFrom = null,
        [FromQuery] string? updatedTo = null,

        // ✅ sort
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDir = "desc"
    )
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        // ✅ excluimos borrados por defecto
        var q = _db.Tickets.AsNoTracking().Where(t => !t.IsDeleted);

        if (status is not null) q = q.Where(t => t.Status == status);
        if (priority is not null) q = q.Where(t => t.Priority == priority);
        if (category is not null) q = q.Where(t => t.Category == category);
        if (type is not null) q = q.Where(t => t.Type == type);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            q = q.Where(t => t.Title.Contains(search) ||
                             (t.Description != null && t.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            createdBy = createdBy.Trim();
            q = q.Where(t => t.CreatedBy.Contains(createdBy));
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            assignedTo = assignedTo.Trim();
            q = q.Where(t => t.AssignedTo != null && t.AssignedTo.Contains(assignedTo));
        }

        // ✅ Fechas (UI manda yyyy-MM-dd)
        var cFrom = ParseDateOnlyAsUtcStart(createdFrom);
        var cTo = ParseDateOnlyAsUtcEndInclusive(createdTo);
        var uFrom = ParseDateOnlyAsUtcStart(updatedFrom);
        var uTo = ParseDateOnlyAsUtcEndInclusive(updatedTo);

        if (cFrom is not null) q = q.Where(t => t.CreatedAtUtc >= cFrom.Value);
        if (cTo is not null) q = q.Where(t => t.CreatedAtUtc <= cTo.Value);
        if (uFrom is not null) q = q.Where(t => t.UpdatedAtUtc >= uFrom.Value);
        if (uTo is not null) q = q.Where(t => t.UpdatedAtUtc <= uTo.Value);

        // ✅ sort
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "updatedat" or "updated" => desc ? q.OrderByDescending(t => t.UpdatedAtUtc) : q.OrderBy(t => t.UpdatedAtUtc),
            "title" => desc ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
            "priority" => desc ? q.OrderByDescending(t => t.Priority) : q.OrderBy(t => t.Priority),
            "status" => desc ? q.OrderByDescending(t => t.Status) : q.OrderBy(t => t.Status),
            "createdby" => desc ? q.OrderByDescending(t => t.CreatedBy) : q.OrderBy(t => t.CreatedBy),
            "assignedto" => desc ? q.OrderByDescending(t => t.AssignedTo) : q.OrderBy(t => t.AssignedTo),
            _ => desc ? q.OrderByDescending(t => t.CreatedAtUtc) : q.OrderBy(t => t.CreatedAtUtc),
        };

        var total = await q.CountAsync();

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                id = t.Id,
                title = t.Title,
                description = t.Description,

                // ✅ CONSISTENTE: strings
                status = t.Status.ToString(),
                priority = t.Priority.ToString(),
                category = t.Category.ToString(),
                type = t.Type.ToString(),

                slaHours = t.SlaHours,
                createdBy = t.CreatedBy,
                assignedTo = t.AssignedTo,
                createdAtUtc = t.CreatedAtUtc,
                updatedAtUtc = t.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    // ============================================================
    // ✅ Soft delete con motivo obligatorio + log + activity
    // ============================================================
    // POST /api/tickets/{id}/delete  { "by": "sebas", "reason": "Duplicado" }
    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> DeleteTicket([FromRoute] Guid id, [FromBody] TicketDeleteRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.By))
            return BadRequest(new { error = "By is required" });

        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { error = "Reason is required" });

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();

        if (ticket.IsDeleted)
            return Ok(new { message = "Already deleted" });

        var by = req.By.Trim();
        var reason = req.Reason.Trim();

        ticket.IsDeleted = true;
        ticket.DeletedAtUtc = DateTime.UtcNow;
        ticket.DeletedBy = by;
        ticket.DeleteReason = reason;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        _db.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            Type = "Deleted",
            Message = reason,
            By = by
        });

        await _db.SaveChangesAsync();

        await _activity.AddAsync(ticket.Id, by, "Deleted",
            $"Borró el ticket. Motivo: {reason}");

        return Ok(new { message = "Deleted", id = ticket.Id });
    }

    // ============================================================
    // ✅ Historial de borrados (para tu página de trazabilidad)
    // ============================================================
    // GET /api/tickets/deleted?page=1&pageSize=20&search=...&deletedBy=...&from=2026-02-01&to=2026-02-15
    [HttpGet("deleted")]
    public async Task<ActionResult<object>> DeletedHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? deletedBy = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] string? sortBy = "deletedAt",
        [FromQuery] string? sortDir = "desc"
    )
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var q = _db.Tickets.AsNoTracking().Where(t => t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            q = q.Where(t =>
                t.Title.Contains(search) ||
                (t.DeleteReason != null && t.DeleteReason.Contains(search))
            );
        }

        if (!string.IsNullOrWhiteSpace(deletedBy))
        {
            deletedBy = deletedBy.Trim();
            q = q.Where(t => t.DeletedBy != null && t.DeletedBy.Contains(deletedBy));
        }

        var dFrom = ParseDateOnlyAsUtcStart(from);
        var dTo = ParseDateOnlyAsUtcEndInclusive(to);

        if (dFrom is not null) q = q.Where(t => t.DeletedAtUtc != null && t.DeletedAtUtc >= dFrom.Value);
        if (dTo is not null) q = q.Where(t => t.DeletedAtUtc != null && t.DeletedAtUtc <= dTo.Value);

        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        q = (sortBy ?? "deletedAt").Trim().ToLowerInvariant() switch
        {
            "title" => desc ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
            "deletedby" => desc ? q.OrderByDescending(t => t.DeletedBy) : q.OrderBy(t => t.DeletedBy),
            _ => desc ? q.OrderByDescending(t => t.DeletedAtUtc) : q.OrderBy(t => t.DeletedAtUtc),
        };

        var total = await q.CountAsync();

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                id = t.Id,
                title = t.Title,
                createdBy = t.CreatedBy,
                assignedTo = t.AssignedTo,

                deletedBy = t.DeletedBy,
                deleteReason = t.DeleteReason,
                deletedAtUtc = t.DeletedAtUtc,

                createdAtUtc = t.CreatedAtUtc,
                updatedAtUtc = t.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    // ============================================================
    // POST /api/tickets
    // ✅ Acepta body en ambos formatos:
    // 1) { "req": { ... } }
    // 2) { ... }
    // ============================================================
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] JsonElement body)
    {
        var payload = body;
        if (body.ValueKind == JsonValueKind.Object &&
            body.TryGetProperty("req", out var reqProp) &&
            reqProp.ValueKind == JsonValueKind.Object)
        {
            payload = reqProp;
        }

        static string? GetString(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var p)) return null;
            if (p.ValueKind == JsonValueKind.String) return p.GetString();
            return p.ToString();
        }

        static bool TryParseEnum<TEnum>(JsonElement obj, string name, out TEnum value) where TEnum : struct, Enum
        {
            value = default;

            if (!obj.TryGetProperty(name, out var p)) return false;

            if (p.ValueKind == JsonValueKind.String)
            {
                var s = p.GetString();
                if (string.IsNullOrWhiteSpace(s)) return false;

                if (int.TryParse(s, out var n))
                {
                    try
                    {
                        value = (TEnum)Enum.ToObject(typeof(TEnum), n);
                        return true;
                    }
                    catch { return false; }
                }

                return Enum.TryParse<TEnum>(s.Trim(), ignoreCase: true, out value);
            }

            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var num))
            {
                try
                {
                    value = (TEnum)Enum.ToObject(typeof(TEnum), num);
                    return true;
                }
                catch { return false; }
            }

            return false;
        }

        var title = GetString(payload, "title")?.Trim();
        var createdBy = GetString(payload, "createdBy")?.Trim();

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { error = "Title is required" });

        if (string.IsNullOrWhiteSpace(createdBy))
            return BadRequest(new { error = "CreatedBy is required" });

        if (!TryParseEnum<TicketPriority>(payload, "priority", out var priority))
            return BadRequest(new { error = "Priority inválida. Usa P1, P2 o P3 (o el valor numérico equivalente)." });

        if (!TryParseEnum<TicketCategory>(payload, "category", out var category))
            return BadRequest(new { error = "Category inválida. Debe ser un valor válido del enum TicketCategory." });

        if (!TryParseEnum<TicketType>(payload, "type", out var type))
            return BadRequest(new { error = "Type inválido. Debe ser un valor válido del enum TicketType." });

        var description = GetString(payload, "description")?.Trim();
        var assignedTo = GetString(payload, "assignedTo")?.Trim();
        assignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo;

        var ticket = new Ticket
        {
            Title = title!,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Priority = priority,
            Status = TicketStatus.Open,

            Category = category,
            Type = type,
            SlaHours = await SlaFromPriorityAsync(priority),

            CreatedBy = createdBy!,
            AssignedTo = assignedTo,
            UpdatedAtUtc = DateTime.UtcNow,

            // ✅ por defecto
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedBy = null,
            DeleteReason = null
        };

        _db.Tickets.Add(ticket);

        _db.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            Type = "Created",
            Message = "Ticket created",
            By = createdBy!
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { error = "Database error", message = "No se pudo crear el ticket." });
        }

        await _activity.AddAsync(ticket.Id, createdBy!, "Created",
            string.IsNullOrWhiteSpace(ticket.AssignedTo)
                ? $"Creó el ticket: \"{ticket.Title}\"."
                : $"Creó el ticket: \"{ticket.Title}\" (asignado a {ticket.AssignedTo}).");

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, await BuildTicketDetails(ticket.Id));
    }

    // GET /api/tickets/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> GetById([FromRoute] Guid id)
    {
        // ✅ no mostrar si está borrado (a menos que quieras permitirlo)
        var exists = await _db.Tickets.AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted);
        if (!exists) return NotFound();

        return Ok(await BuildTicketDetails(id));
    }

    // GET /api/tickets/{id}/events
    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<object>> Events([FromRoute] Guid id)
    {
        var ticketExists = await _db.Tickets.AsNoTracking().AnyAsync(t => t.Id == id);
        if (!ticketExists) return NotFound();

        var events = await _db.TicketEvents.AsNoTracking()
            .Where(e => e.TicketId == id)
            .OrderByDescending(e => e.AtUtc)
            .Select(e => new { e.Id, e.Type, e.Message, e.By, e.AtUtc })
            .ToListAsync();

        return Ok(new { ticketId = id, events });
    }

    // ==========================
    // ✅ EVIDENCE (acepta api/v1 o api, singular o plural)
    // ==========================
    [HttpGet("{id:guid}/evidence")]
    [HttpGet("{id:guid}/evidences")]
    public async Task<ActionResult<TicketEvidenceListResponse>> ListEvidence([FromRoute] Guid id, CancellationToken ct)
    {
        var exists = await _db.Tickets.AnyAsync(t => t.Id == id, ct);
        if (!exists) return NotFound("Ticket not found.");

        var items = await _db.TicketEvidences.AsNoTracking()
            .Where(x => x.TicketId == id)
            .OrderByDescending(x => x.UploadedAtUtc)
            .Select(x => new TicketEvidenceItem(
                x.Id, x.TicketId, x.FileName, x.ContentType, x.SizeBytes, x.UploadedBy, x.UploadedAtUtc
            ))
            .ToListAsync(ct);

        return Ok(new TicketEvidenceListResponse(items));
    }

    [HttpPost("{id:guid}/evidence")]
    [HttpPost("{id:guid}/evidences")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadEvidence(
        [FromRoute] Guid id,
        [FromForm] IFormFile? file,
        [FromForm] List<IFormFile>? files,
        [FromForm] string? by,
        [FromForm] string? comment, // ✅ NUEVO (opcional)
        CancellationToken ct)
    {
        var list = new List<IFormFile>();
        if (files is { Count: > 0 }) list.AddRange(files);
        if (file != null) list.Add(file);

        if (list.Count == 0)
            return BadRequest("No files.");

        var exists = await _db.Tickets.AnyAsync(t => t.Id == id, ct);
        if (!exists) return NotFound("Ticket not found.");

        var uploader = string.IsNullOrWhiteSpace(by) ? "system" : by.Trim();
        var cmt = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "evidence");
        Directory.CreateDirectory(root);

        foreach (var f in list)
        {
            if (f.Length <= 0) continue;

            var safeName = Path.GetFileName(f.FileName);
            var ext = Path.GetExtension(safeName);

            var evidenceId = Guid.NewGuid();
            var storedFileName = $"{id:N}_{evidenceId:N}{ext}";
            var fullPath = Path.Combine(root, storedFileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await f.CopyToAsync(stream, ct);
            }

            _db.TicketEvidences.Add(new TicketEvidence
            {
                Id = evidenceId,
                TicketId = id,
                FileName = safeName,
                ContentType = string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType,
                SizeBytes = f.Length,
                StoragePath = storedFileName,
                UploadedBy = uploader,
                UploadedAtUtc = DateTime.UtcNow,

                // ✅ requiere que agregues Comment en entidad/migración
                Comment = cmt
            });

            _db.TicketEvents.Add(new TicketEvent
            {
                TicketId = id,
                Type = "EvidenceUploaded",
                Message = string.IsNullOrWhiteSpace(cmt) ? $"Uploaded: {safeName}" : $"Uploaded: {safeName} | Comment: {cmt}",
                By = uploader
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("evidence/{evidenceId:guid}/download")]
    public async Task<IActionResult> DownloadEvidence([FromRoute] Guid evidenceId, CancellationToken ct)
    {
        var ev = await _db.TicketEvidences.AsNoTracking().FirstOrDefaultAsync(x => x.Id == evidenceId, ct);
        if (ev is null) return NotFound();

        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "evidence");
        var fullPath = Path.Combine(root, ev.StoragePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("File missing on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, ct);
        return File(bytes, ev.ContentType, ev.FileName);
    }

    // ============================================================
    // ✅ DELETE EVIDENCE (simple) - lo dejas por compat
    // ============================================================
    [HttpDelete("evidence/{evidenceId:guid}")]
    public async Task<IActionResult> DeleteEvidence([FromRoute] Guid evidenceId, CancellationToken ct)
    {
        var ev = await _db.TicketEvidences.FirstOrDefaultAsync(x => x.Id == evidenceId, ct);
        if (ev is null) return NotFound(new { error = "Evidence not found" });

        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "evidence");
        var fullPath = Path.Combine(root, ev.StoragePath);

        try
        {
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to delete file from disk", detail = ex.Message });
        }

        _db.TicketEvidences.Remove(ev);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = evidenceId });
    }

    // ============================================================
    // ✅ DELETE EVIDENCE PRO (con trazabilidad: by + reason)
    // ============================================================
    // POST /api/tickets/{ticketId}/evidence/{evidenceId}/delete
    [HttpPost("{ticketId:guid}/evidence/{evidenceId:guid}/delete")]
    public async Task<IActionResult> DeleteEvidencePro(
        [FromRoute] Guid ticketId,
        [FromRoute] Guid evidenceId,
        [FromBody] TicketEvidenceDeleteRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.By))
            return BadRequest(new { error = "By is required" });

        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { error = "Reason is required" });

        var ev = await _db.TicketEvidences.FirstOrDefaultAsync(x => x.Id == evidenceId && x.TicketId == ticketId, ct);
        if (ev is null) return NotFound(new { error = "Evidence not found" });

        var by = req.By.Trim();
        var reason = req.Reason.Trim();

        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "evidence");
        var fullPath = Path.Combine(root, ev.StoragePath);

        try
        {
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to delete file from disk", detail = ex.Message });
        }

        _db.TicketEvidences.Remove(ev);

        _db.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticketId,
            Type = "EvidenceDeleted",
            Message = $"Evidence {evidenceId} deleted. Reason: {reason}",
            By = by
        });

        await _db.SaveChangesAsync(ct);

        await _activity.AddAsync(ticketId, by, "EvidenceDeleted",
            $"Borró evidencia ({evidenceId}). Motivo: {reason}");

        return Ok(new { ok = true, id = evidenceId });
    }

    // DELETE /api/v1/tickets/{ticketId}/attachments/{attachmentId}
    // compat para el Web (attachments = TicketEvidences)
    [HttpDelete("{ticketId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment([FromRoute] Guid ticketId, [FromRoute] Guid attachmentId, CancellationToken ct)
    {
        var ev = await _db.TicketEvidences.FirstOrDefaultAsync(
            x => x.Id == attachmentId && x.TicketId == ticketId, ct);

        if (ev is null) return NotFound(new { error = "Attachment not found" });

        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "evidence");
        var fullPath = Path.Combine(root, ev.StoragePath);

        try
        {
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to delete file from disk", detail = ex.Message });
        }

        _db.TicketEvidences.Remove(ev);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = attachmentId });
    }

    // ============================================================
    // ✅ Compatibility endpoints para tu MVC (attachments)
    // - Tu Web hoy llama /attachments y /downloadAttachment
    // - Esto lo dejamos compatible sin tocar el Web
    // ============================================================

    // GET /api/tickets/{id}/attachments  -> lista evidencias
    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> Attachments([FromRoute] Guid id, CancellationToken ct)
    {
        var exists = await _db.Tickets.AnyAsync(t => t.Id == id, ct);
        if (!exists) return NotFound("Ticket not found.");

        var items = await _db.TicketEvidences.AsNoTracking()
            .Where(x => x.TicketId == id)
            .OrderByDescending(x => x.UploadedAtUtc)
            .Select(x => new
            {
                id = x.Id,
                ticketId = x.TicketId,
                fileName = x.FileName,
                contentType = x.ContentType,
                sizeBytes = x.SizeBytes,
                uploadedBy = x.UploadedBy,
                uploadedAtUtc = x.UploadedAtUtc,

                // ✅ si agregas Comment
                comment = x.Comment
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    // GET /api/tickets/{ticketId}/attachments/{attachmentId}/download
    [HttpGet("{ticketId:guid}/attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment([FromRoute] Guid ticketId, [FromRoute] Guid attachmentId, CancellationToken ct)
    {
        var ev = await _db.TicketEvidences.AsNoTracking().FirstOrDefaultAsync(x => x.Id == attachmentId, ct);
        if (ev is null) return NotFound();

        var root = Path.Combine(AppContext.BaseDirectory, "App_Data", "evidence");
        var fullPath = Path.Combine(root, ev.StoragePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("File missing on disk.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath, ct);
        return File(bytes, ev.ContentType, ev.FileName);
    }

    // ==========================
    // ✅ EDIT / PATCH / CLOSE / COMMENTS / REOPEN (igual que tenías)
    // ==========================
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<object>> Edit([FromRoute] Guid id, [FromBody] TicketUpdateRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.By))
            return BadRequest(new { error = "By is required" });

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "Title is required" });

        if (!Enum.TryParse<TicketPriority>(req.Priority, ignoreCase: true, out var parsedPriority))
            return BadRequest(new { error = "Priority inválida. Usa P1, P2 o P3" });

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (ticket is null) return NotFound();

        var by = req.By.Trim();
        var changed = false;

        var newTitle = req.Title.Trim();
        if (ticket.Title != newTitle)
        {
            var old = ticket.Title;
            ticket.Title = newTitle;

            _db.TicketEvents.Add(new TicketEvent
            {
                TicketId = ticket.Id,
                Type = "TitleChanged",
                Message = $"Title: {old} -> {ticket.Title}",
                By = by
            });

            await _activity.AddAsync(ticket.Id, by, "TitleChanged",
                $"Cambió el título: \"{old}\" → \"{ticket.Title}\".");

            changed = true;
        }

        var newDesc = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        if (ticket.Description != newDesc)
        {
            var old = ticket.Description ?? "(vacío)";
            var now = newDesc ?? "(vacío)";
            ticket.Description = newDesc;

            _db.TicketEvents.Add(new TicketEvent
            {
                TicketId = ticket.Id,
                Type = "DescriptionChanged",
                Message = $"Description: {old} -> {now}",
                By = by
            });

            await _activity.AddAsync(ticket.Id, by, "DescriptionChanged", "Actualizó la descripción.");
            changed = true;
        }

        if (ticket.Priority != parsedPriority)
        {
            var old = ticket.Priority;
            ticket.Priority = parsedPriority;

            ticket.SlaHours = await SlaFromPriorityAsync(parsedPriority);

            _db.TicketEvents.Add(new TicketEvent
            {
                TicketId = ticket.Id,
                Type = "PriorityChanged",
                Message = $"Priority: {old} -> {ticket.Priority}",
                By = by
            });

            await _activity.AddAsync(ticket.Id, by, "PriorityChanged",
                $"Cambió prioridad: {old} → {ticket.Priority}.");

            changed = true;
        }

        if (req.AssignedTo is not null)
        {
            var newAssignee = string.IsNullOrWhiteSpace(req.AssignedTo) ? null : req.AssignedTo.Trim();
            if (ticket.AssignedTo != newAssignee)
            {
                var old = ticket.AssignedTo ?? "(sin asignar)";
                var now = newAssignee ?? "(sin asignar)";
                ticket.AssignedTo = newAssignee;

                _db.TicketEvents.Add(new TicketEvent
                {
                    TicketId = ticket.Id,
                    Type = "Assigned",
                    Message = $"AssignedTo: {old} -> {now}",
                    By = by
                });

                await _activity.AddAsync(ticket.Id, by, "Assigned",
                    $"Cambió asignación: {old} → {now}.");

                changed = true;
            }
        }

        if (!changed)
            return Ok(await BuildTicketDetails(ticket.Id));

        ticket.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Concurrency conflict", message = "Reintenta." });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { error = "Database error", message = "No se pudo editar el ticket." });
        }

        return Ok(await BuildTicketDetails(ticket.Id));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<object>> Patch([FromRoute] Guid id, [FromBody] TicketPatchRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.By))
            return BadRequest(new { error = "By is required" });

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (ticket is null) return NotFound();

        var by = req.By.Trim();
        var changed = false;

        if (req.Status is not null && ticket.Status != req.Status.Value)
        {
            var old = ticket.Status;
            ticket.Status = req.Status.Value;

            _db.TicketEvents.Add(new TicketEvent
            {
                TicketId = ticket.Id,
                Type = "StatusChanged",
                Message = $"Status: {old} -> {ticket.Status}",
                By = by
            });

            await _activity.AddAsync(ticket.Id, by, "StatusChanged",
                $"Cambió estado: {old} → {ticket.Status}.");

            changed = true;
        }

        if (req.Priority is not null && ticket.Priority != req.Priority.Value)
        {
            var old = ticket.Priority;
            ticket.Priority = req.Priority.Value;

            ticket.SlaHours = await SlaFromPriorityAsync(ticket.Priority);

            _db.TicketEvents.Add(new TicketEvent
            {
                TicketId = ticket.Id,
                Type = "PriorityChanged",
                Message = $"Priority: {old} -> {ticket.Priority}",
                By = by
            });

            await _activity.AddAsync(ticket.Id, by, "PriorityChanged",
                $"Cambió prioridad: {old} → {ticket.Priority}.");

            changed = true;
        }

        if (req.AssignedTo is not null)
        {
            var newAssignee = string.IsNullOrWhiteSpace(req.AssignedTo) ? null : req.AssignedTo.Trim();
            if (ticket.AssignedTo != newAssignee)
            {
                var old = ticket.AssignedTo ?? "(sin asignar)";
                var now = newAssignee ?? "(sin asignar)";
                ticket.AssignedTo = newAssignee;

                _db.TicketEvents.Add(new TicketEvent
                {
                    TicketId = ticket.Id,
                    Type = "Assigned",
                    Message = $"AssignedTo: {old} -> {now}",
                    By = by
                });

                await _activity.AddAsync(ticket.Id, by, "Assigned",
                    $"Cambió asignación: {old} → {now}.");

                changed = true;
            }
        }

        if (!changed)
            return Ok(new { message = "No changes" });

        ticket.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                error = "Concurrency conflict",
                message = "El ticket cambió en la base de datos. Reintenta el PATCH."
            });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { error = "Database error", message = "No se pudo actualizar el ticket." });
        }

        return Ok(await BuildTicketDetails(ticket.Id));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<object>> Close([FromRoute] Guid id, [FromBody] TicketCloseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.By))
            return BadRequest(new { error = "By is required" });

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (ticket is null) return NotFound();

        if (ticket.Status == TicketStatus.Closed)
            return Ok(new { message = "Already closed" });

        var by = req.By.Trim();

        void ApplyCloseAndAddEvent()
        {
            ticket.Status = TicketStatus.Closed;
            ticket.UpdatedAtUtc = DateTime.UtcNow;

            _db.TicketEvents.Add(new TicketEvent
            {
                TicketId = ticket.Id,
                Type = "Closed",
                Message = string.IsNullOrWhiteSpace(req.Comment)
                    ? "Ticket closed"
                    : $"Ticket closed: {req.Comment.Trim()}",
                By = by
            });
        }

        ApplyCloseAndAddEvent();

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await _db.SaveChangesAsync();
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt == 1)
            {
                await _db.Entry(ticket).ReloadAsync();
                ApplyCloseAndAddEvent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { error = "Concurrency conflict", message = "Reintenta." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { error = "Database error", message = "No se pudo cerrar el ticket." });
            }
        }

        await _activity.AddAsync(ticket.Id, by, "Closed",
            string.IsNullOrWhiteSpace(req.Comment)
                ? "Cerró el ticket."
                : $"Cerró el ticket: {req.Comment.Trim()}");

        return Ok(await BuildTicketDetails(ticket.Id));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<object>> AddComment([FromRoute] Guid id, [FromBody] TicketCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.By))
            return BadRequest(new { error = "By is required" });

        if (string.IsNullOrWhiteSpace(req.Comment))
            return BadRequest(new { error = "Comment is required" });

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (ticket is null) return NotFound();

        ticket.UpdatedAtUtc = DateTime.UtcNow;

        _db.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            Type = "CommentAdded",
            Message = req.Comment.Trim(),
            By = req.By.Trim()
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Concurrency conflict", message = "Reintenta." });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { error = "Database error", message = "No se pudo agregar el comentario." });
        }

        await _activity.AddAsync(ticket.Id, req.By.Trim(), "CommentAdded",
            $"Agregó un comentario: {req.Comment.Trim()}");

        return Ok(new { message = "Comment added" });
    }

    [HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<object>> Reopen([FromRoute] Guid id, [FromBody] TicketCloseRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.By))
            return BadRequest(new { error = "By is required" });

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (ticket is null) return NotFound();

        if (ticket.Status != TicketStatus.Closed)
            return Ok(new { message = "Not closed (no need to reopen)" });

        ticket.Status = TicketStatus.Open;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        _db.TicketEvents.Add(new TicketEvent
        {
            TicketId = ticket.Id,
            Type = "Reopened",
            Message = string.IsNullOrWhiteSpace(req.Comment)
                ? "Ticket reopened"
                : $"Ticket reopened: {req.Comment.Trim()}",
            By = req.By.Trim()
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Concurrency conflict", message = "Reintenta." });
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new { error = "Database error", message = "No se pudo reabrir el ticket." });
        }

        await _activity.AddAsync(ticket.Id, req.By.Trim(), "Reopened",
            string.IsNullOrWhiteSpace(req.Comment)
                ? "Reabrió el ticket."
                : $"Reabrió el ticket: {req.Comment.Trim()}");

        return Ok(await BuildTicketDetails(ticket.Id));
    }

    // ✅ helper: devuelve ticket + activities + comments
    private async Task<object> BuildTicketDetails(Guid id)
    {
        // ✅ si quieres permitir ver borrados en details, quita el !t.IsDeleted
        var t = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null) return new { };

        var activities = await _db.TicketActivities.AsNoTracking()
            .Where(a => a.TicketId == id)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new
            {
                a.CreatedAtUtc,
                a.By,
                a.Action,
                a.Message
            })
            .ToListAsync();

        var comments = await _db.TicketEvents.AsNoTracking()
            .Where(e => e.TicketId == id && e.Type == "CommentAdded")
            .OrderByDescending(e => e.AtUtc)
            .Select(e => new
            {
                e.AtUtc,
                e.By,
                Message = e.Message
            })
            .ToListAsync();

        return new
        {
            t.Id,
            t.Title,
            t.Description,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),

            Category = t.Category.ToString(),
            Type = t.Type.ToString(),
            t.SlaHours,

            t.CreatedBy,
            t.AssignedTo,
            t.CreatedAtUtc,
            t.UpdatedAtUtc,

            Activities = activities,
            Comments = comments
        };
    }
}
