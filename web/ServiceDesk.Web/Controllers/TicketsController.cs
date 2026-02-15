// C:\ProyectoASPNET\servicedesk\web\ServiceDesk.Web\Controllers\TicketsController.cs

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Web.Models.Tickets;

namespace ServiceDesk.Web.Controllers;

public class TicketsController : Controller
{
    private readonly IHttpClientFactory _http;

    // ✅ Fallback SOLO si no hay usuario logueado (dev / pruebas)
    private const string FallbackBy = "system";

    // ✅ Lista fija (manual por ahora) — luego la traemos desde Identity/API
    private static readonly List<string> Assignees = new()
    {
        "sebas",
        "soporte",
        "juan",
        "maria"
    };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TicketsController(IHttpClientFactory http)
    {
        _http = http;
    }

    // ============================
    // ✅ PRO: Usuario logueado (BY / CreatedBy)
    // ============================
    // Tu app hoy usa Session (AuthController guarda access_token, roles, display_name)
    // Así que acá leemos Session y NO dependemos de User.Identity.Name.
    private string CurrentBy()
    {
        // 1) Display name (lo que muestras arriba: "juancito")
        var display = HttpContext?.Session?.GetString("display_name");
        if (!string.IsNullOrWhiteSpace(display))
            return display.Trim();

        // 2) Username (si lo guardas en Session)
        var username = HttpContext?.Session?.GetString("username");
        if (!string.IsNullOrWhiteSpace(username))
            return username.Trim();

        // 3) Email (si lo guardas en Session)
        var email = HttpContext?.Session?.GetString("email");
        if (!string.IsNullOrWhiteSpace(email))
            return email.Trim();

        return FallbackBy;
    }

    // ============================
    // Helpers (query builder + api fallback)
    // ============================

    private static string BuildTicketsQuery(
        int page,
        int pageSize,
        string? status,
        string? priority,
        string? category,
        string? type,
        string? search,
        string? createdBy,
        string? assignedTo,
        string? createdFrom,
        string? createdTo,
        string? updatedFrom,
        string? updatedTo,
        string? sortBy,
        string? sortDir)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        // Preferimos /api/v1 primero
        var url = $"api/v1/tickets?page={page}&pageSize={pageSize}";

        void Add(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            url += $"&{key}={Uri.EscapeDataString(value.Trim())}";
        }

        Add("status", status);
        Add("priority", priority);
        Add("category", category);
        Add("type", type);
        Add("search", search);

        Add("createdBy", createdBy);
        Add("assignedTo", assignedTo);

        Add("createdFrom", createdFrom);
        Add("createdTo", createdTo);
        Add("updatedFrom", updatedFrom);
        Add("updatedTo", updatedTo);

        Add("sortBy", string.IsNullOrWhiteSpace(sortBy) ? "createdAt" : sortBy);
        Add("sortDir", string.IsNullOrWhiteSpace(sortDir) ? "desc" : sortDir);

        return url;
    }

    private static string ApiFallback(string urlV1)
    {
        return urlV1.Replace("api/v1/tickets", "api/tickets");
    }

    private static void NoCache(HttpResponse response)
    {
        response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";
    }

    // ============================
    // ✅ LISTADO (server-render)
    // ============================

    // GET: /Tickets
    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? priority = null,
        string? category = null,
        string? type = null,
        string? search = null,

        // ✅ Advanced
        string? createdBy = null,
        string? assignedTo = null,
        string? createdFrom = null,
        string? createdTo = null,
        string? updatedFrom = null,
        string? updatedTo = null,
        string? sortBy = "createdAt",
        string? sortDir = "desc")
    {
        NoCache(Response);

        var client = _http.CreateClient("api");

        // ViewBag para mantener filtros en la vista
        ViewBag.Status = status;
        ViewBag.Priority = priority;
        ViewBag.Category = category;
        ViewBag.Type = type;
        ViewBag.Search = search;

        ViewBag.CreatedBy = createdBy;
        ViewBag.AssignedTo = assignedTo;
        ViewBag.CreatedFrom = createdFrom;
        ViewBag.CreatedTo = createdTo;
        ViewBag.UpdatedFrom = updatedFrom;
        ViewBag.UpdatedTo = updatedTo;
        ViewBag.SortBy = sortBy;
        ViewBag.SortDir = sortDir;

        var urlV1 = BuildTicketsQuery(
            page, pageSize,
            status, priority, category, type, search,
            createdBy, assignedTo,
            createdFrom, createdTo, updatedFrom, updatedTo,
            sortBy, sortDir);

        var res = await client.GetAsync(urlV1);

        if ((int)res.StatusCode == 404)
            res = await client.GetAsync(ApiFallback(urlV1));

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            ViewBag.Error = $"No pude cargar tickets. Status: {(int)res.StatusCode}. Body: {body}";
            return View(new PagedTicketsResponse(page, pageSize, 0, new()));
        }

        var data = await res.Content.ReadFromJsonAsync<PagedTicketsResponse>(JsonOpts);
        return View(data ?? new PagedTicketsResponse(page, pageSize, 0, new()));
    }

    // ✅ Endpoint MVC para AJAX (tickets.js)
    [HttpGet]
    public async Task<IActionResult> AjaxList(
        int page = 1,
        int pageSize = 20,
        string? status = null,
        string? priority = null,
        string? category = null,
        string? type = null,
        string? search = null,

        // ✅ Advanced
        string? createdBy = null,
        string? assignedTo = null,
        string? createdFrom = null,
        string? createdTo = null,
        string? updatedFrom = null,
        string? updatedTo = null,
        string? sortBy = "createdAt",
        string? sortDir = "desc")
    {
        NoCache(Response);

        var client = _http.CreateClient("api");

        var urlV1 = BuildTicketsQuery(
            page, pageSize,
            status, priority, category, type, search,
            createdBy, assignedTo,
            createdFrom, createdTo, updatedFrom, updatedTo,
            sortBy, sortDir);

        var res = await client.GetAsync(urlV1);

        if ((int)res.StatusCode == 404)
            res = await client.GetAsync(ApiFallback(urlV1));

        if (!res.IsSuccessStatusCode)
        {
            var bodyErr = await res.Content.ReadAsStringAsync();
            return StatusCode((int)res.StatusCode, bodyErr);
        }

        var data = await res.Content.ReadFromJsonAsync<PagedTicketsResponse>(JsonOpts);
        if (data is null)
            return Json(new { page = 1, pageSize = 20, total = 0, items = Array.Empty<object>() });

        var mapped = new
        {
            page = data.Page,
            pageSize = data.PageSize,
            total = data.Total,
            items = data.Items.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                description = t.Description,

                statusRaw = t.Status,
                priorityRaw = t.Priority,

                status = (t.Status ?? "Open").Trim(),
                priority = (t.Priority ?? "P3").Trim(),
                category = (t.Category ?? "Software").Trim(),
                type = (t.Type ?? "Incidencia").Trim(),

                createdBy = t.CreatedBy,
                assignedTo = t.AssignedTo,
                createdAtUtc = t.CreatedAtUtc,
                updatedAtUtc = t.UpdatedAtUtc
            })
        };

        return Json(mapped);
    }

    // ============================
    // ✅ HISTORIAL DE BORRADOS
    // ============================

    public IActionResult DeletedHistory() => View();

    [HttpGet]
    public async Task<IActionResult> AjaxDeletedHistory(int page = 1, int pageSize = 20)
    {
        NoCache(Response);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var client = _http.CreateClient("api");

        var urlV1 = $"api/v1/tickets/deleted?page={page}&pageSize={pageSize}";
        var res = await client.GetAsync(urlV1);

        if ((int)res.StatusCode == 404)
            res = await client.GetAsync(urlV1.Replace("api/v1/tickets", "api/tickets"));

        if (!res.IsSuccessStatusCode)
        {
            var bodyErr = await res.Content.ReadAsStringAsync();
            return StatusCode((int)res.StatusCode, bodyErr);
        }

        var raw = await res.Content.ReadAsStringAsync();
        return Content(raw, "application/json");
    }

    // ============================
    // ✅ BORRAR TICKET con motivo (AJAX)
    // ============================

    public record TicketDeleteWebRequest(string? Reason);

    [HttpPost("/Tickets/Delete/{id:guid}")]
    [ValidateAntiForgeryToken] // ✅ IMPORTANTÍSIMO: protege POST desde fetch
    public async Task<IActionResult> Delete([FromRoute] Guid id, [FromBody] TicketDeleteWebRequest req)
    {
        var reason = (req?.Reason ?? "").Trim();
        if (string.IsNullOrWhiteSpace(reason))
            return BadRequest(new { error = "Reason is required" });

        var by = CurrentBy();

        var client = _http.CreateClient("api");
        var payload = new { by, reason };

        var res = await client.PostAsJsonAsync($"api/v1/tickets/{id}/delete", payload);
        if ((int)res.StatusCode == 404)
            res = await client.PostAsJsonAsync($"api/tickets/{id}/delete", payload);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            return StatusCode((int)res.StatusCode, body);
        }

        return Ok(new { ok = true, id });
    }

    // ============================
    // ✅ DETAILS
    // ============================

    public async Task<IActionResult> Details(Guid id)
    {
        var client = _http.CreateClient("api");

        var res = await client.GetAsync($"api/v1/tickets/{id}");
        if ((int)res.StatusCode == 404)
            res = await client.GetAsync($"api/tickets/{id}");

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            ViewBag.Error = $"No pude cargar el ticket. Status: {(int)res.StatusCode}. Body: {body}";
            return View(null);
        }

        var data = await res.Content.ReadFromJsonAsync<TicketDetailsResponse>(JsonOpts);
        return View(data);
    }

    // ============================
    // ✅ CREATE (PRO)
    // ============================

    public IActionResult Create()
    {
        ViewBag.Assignees = Assignees;

        // ✅ para que la vista pueda mostrar "Creado por"
        var by = CurrentBy();
        ViewBag.CreatedByText = by;

        return View(new TicketCreateRequest
        {
            CreatedBy = by,
            Priority = "P2",
            Category = "Software",
            Type = "Incidencia",
            AssignedTo = "soporte"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TicketCreateRequest model,
        string? EvidenceBy,
        List<IFormFile>? EvidenceFiles)
    {
        ViewBag.Assignees = Assignees;

        model.CreatedBy = CurrentBy();
        ModelState.Remove(nameof(model.CreatedBy));

        ViewBag.CreatedByText = model.CreatedBy;

        if (!ModelState.IsValid)
            return View(model);

        model.Priority = string.IsNullOrWhiteSpace(model.Priority) ? "P2" : model.Priority.Trim();
        model.Category = string.IsNullOrWhiteSpace(model.Category) ? "Software" : model.Category.Trim();
        model.Type = string.IsNullOrWhiteSpace(model.Type) ? "Incidencia" : model.Type.Trim();

        if (!string.IsNullOrWhiteSpace(model.AssignedTo))
            model.AssignedTo = model.AssignedTo.Trim();

        model.Title = model.Title?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(model.Description))
            model.Description = model.Description.Trim();

        var client = _http.CreateClient("api");

        var res = await client.PostAsJsonAsync("api/v1/tickets", model);
        if ((int)res.StatusCode == 404)
            res = await client.PostAsJsonAsync("api/tickets", model);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            ViewBag.Error = $"No pude crear el ticket. Status: {(int)res.StatusCode}. Body: {body}";
            return View(model);
        }

        var created = await res.Content.ReadFromJsonAsync<TicketCreateResponse>(JsonOpts);
        TempData["Success"] = "✅ Ticket creado correctamente.";

        if (created is not null && EvidenceFiles is { Count: > 0 })
        {
            var by = CurrentBy();

            var uploadOk = await UploadEvidenceAsync(client, created.Id, EvidenceFiles, by);
            if (!uploadOk.ok)
                TempData["Error"] = $"Ticket creado, pero evidencia no se pudo subir: {uploadOk.error}";
        }

        if (created is not null)
            return RedirectToAction("Details", new { id = created.Id });

        return RedirectToAction("Index");
    }

    // ============================
    // ✅ EDIT (PRO: By automático)
    // ============================

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = _http.CreateClient("api");

        var res = await client.GetAsync($"api/v1/tickets/{id}");
        if ((int)res.StatusCode == 404)
            res = await client.GetAsync($"api/tickets/{id}");

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude cargar el ticket para editar. Status: {(int)res.StatusCode}. Body: {body}";
            return RedirectToAction("Details", new { id });
        }

        var data = await res.Content.ReadFromJsonAsync<TicketDetailsResponse>(JsonOpts);
        if (data is null)
        {
            TempData["Error"] = "Ticket no encontrado.";
            return RedirectToAction("Index");
        }

        ViewBag.Assignees = Assignees;

        var vm = new TicketUpdateRequest
        {
            Title = data.Title,
            Description = data.Description,
            Priority = data.Priority,
            By = CurrentBy(),
            AssignedTo = data.AssignedTo
        };

        ViewBag.TicketId = id;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        TicketUpdateRequest model,
        string? EvidenceBy,
        List<IFormFile>? EvidenceFiles)
    {
        ViewBag.Assignees = Assignees;
        ViewBag.TicketId = id;

        if (!ModelState.IsValid)
            return View(model);

        model.By = CurrentBy();

        var client = _http.CreateClient("api");

        var res = await client.PutAsJsonAsync($"api/v1/tickets/{id}", model);
        if ((int)res.StatusCode == 404)
            res = await client.PutAsJsonAsync($"api/tickets/{id}", model);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude guardar cambios. Status: {(int)res.StatusCode}. Body: {body}";
            return RedirectToAction("Edit", new { id });
        }

        if (EvidenceFiles is { Count: > 0 })
        {
            var by = CurrentBy();
            var uploadOk = await UploadEvidenceAsync(client, id, EvidenceFiles, by);

            if (!uploadOk.ok)
                TempData["Error"] = $"Ticket actualizado, pero evidencia no se pudo subir: {uploadOk.error}";
            else
                TempData["Success"] = "✅ Ticket actualizado + evidencia subida.";
        }
        else
        {
            TempData["Success"] = "✅ Ticket actualizado.";
        }

        return RedirectToAction("Details", new { id });
    }

    // ============================
    // ✅ ACTIONS: CLOSE / REOPEN / COMMENT
    // ============================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id)
    {
        var client = _http.CreateClient("api");
        var by = CurrentBy();

        var res = await client.PostAsJsonAsync($"api/v1/tickets/{id}/close", new TicketActionRequest(by));
        if ((int)res.StatusCode == 404)
            res = await client.PostAsJsonAsync($"api/tickets/{id}/close", new TicketActionRequest(by));

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude cerrar el ticket. Status: {(int)res.StatusCode}. Body: {body}";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "✅ Ticket cerrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id)
    {
        var client = _http.CreateClient("api");
        var by = CurrentBy();

        var res = await client.PostAsJsonAsync($"api/v1/tickets/{id}/reopen", new TicketActionRequest(by));
        if ((int)res.StatusCode == 404)
            res = await client.PostAsJsonAsync($"api/tickets/{id}/reopen", new TicketActionRequest(by));

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude reabrir el ticket. Status: {(int)res.StatusCode}. Body: {body}";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "✅ Ticket reabierto.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid id, string By, string Comment)
    {
        if (string.IsNullOrWhiteSpace(Comment))
        {
            TempData["Error"] = "Comment es obligatorio.";
            return RedirectToAction("Details", new { id });
        }

        var client = _http.CreateClient("api");
        var by = CurrentBy();

        var res = await client.PostAsJsonAsync(
            $"api/v1/tickets/{id}/comments",
            new TicketCommentRequest(by, Comment.Trim())
        );

        if ((int)res.StatusCode == 404)
        {
            res = await client.PostAsJsonAsync(
                $"api/tickets/{id}/comments",
                new TicketCommentRequest(by, Comment.Trim())
            );
        }

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude agregar comentario. Status: {(int)res.StatusCode}. Body: {body}";
        }
        else
        {
            TempData["Success"] = "✅ Comentario agregado.";
        }

        return RedirectToAction("Details", new { id });
    }

    // ============================
    // ✅ EVIDENCIA (UPLOAD + LIST + DOWNLOAD)
    // ============================

    private static async Task<(bool ok, string? error)> UploadEvidenceAsync(
        HttpClient client,
        Guid ticketId,
        List<IFormFile> files,
        string by)
    {
        try
        {
            var routesToTry = new[]
            {
                $"api/v1/tickets/{ticketId}/evidence",
                $"api/v1/tickets/{ticketId}/evidences",
                $"api/tickets/{ticketId}/evidence",
                $"api/tickets/{ticketId}/evidences",
            };

            var anyReal = files.Any(f => f is not null && f.Length > 0);
            if (!anyReal)
                return (true, null);

            foreach (var route in routesToTry)
            {
                using var form = new MultipartFormDataContent();

                if (!string.IsNullOrWhiteSpace(by))
                    form.Add(new StringContent(by.Trim()), "by");

                foreach (var f in files)
                {
                    if (f is null || f.Length <= 0) continue;

                    var streamContent = new StreamContent(f.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                        string.IsNullOrWhiteSpace(f.ContentType) ? "application/octet-stream" : f.ContentType
                    );

                    form.Add(streamContent, "files", f.FileName);
                }

                var res = await client.PostAsync(route, form);

                if (res.IsSuccessStatusCode)
                    return (true, null);

                if ((int)res.StatusCode == 404 || (int)res.StatusCode == 405)
                    continue;

                var body = await res.Content.ReadAsStringAsync();
                return (false, $"Ruta '{route}' devolvió Status {(int)res.StatusCode}. Body: {body}");
            }

            return (false, "No hay endpoint de evidencia (probé evidence/evidences con y sin /api/v1).");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    [HttpPost("/Tickets/UploadEvidenceAjax/{ticketId:guid}")]
    public async Task<IActionResult> UploadEvidenceAjax([FromRoute] Guid ticketId)
    {
        try
        {
            var by = CurrentBy();

            var files = Request.Form.Files.ToList();
            if (files.Count == 0)
                return BadRequest("No files.");

            var client = _http.CreateClient("api");
            var uploadOk = await UploadEvidenceAsync(client, ticketId, files, by);

            if (!uploadOk.ok)
                return StatusCode(400, uploadOk.error ?? "Upload failed");

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("/Tickets/Attachments/{ticketId:guid}")]
    public async Task<IActionResult> Attachments([FromRoute] Guid ticketId)
    {
        var client = _http.CreateClient("api");

        var routesToTry = new[]
        {
            $"api/v1/tickets/{ticketId}/attachments",
            $"api/v1/tickets/{ticketId}/evidence",
            $"api/v1/tickets/{ticketId}/evidences",
            $"api/tickets/{ticketId}/attachments",
            $"api/tickets/{ticketId}/evidence",
            $"api/tickets/{ticketId}/evidences",
        };

        foreach (var route in routesToTry)
        {
            var res = await client.GetAsync(route);

            if (res.IsSuccessStatusCode)
            {
                var text = await res.Content.ReadAsStringAsync();
                return Content(text, "application/json");
            }

            if ((int)res.StatusCode == 404 || (int)res.StatusCode == 405)
                continue;

            var body = await res.Content.ReadAsStringAsync();
            return StatusCode((int)res.StatusCode, body);
        }

        return StatusCode(404, "No hay endpoint de attachments/evidence en la API.");
    }

    [HttpGet("/Tickets/DownloadAttachment/{ticketId:guid}/{attachmentId:guid}")]
    public async Task<IActionResult> DownloadAttachment([FromRoute] Guid ticketId, [FromRoute] Guid attachmentId)
    {
        var client = _http.CreateClient("api");

        var routesToTry = new[]
        {
            $"api/v1/tickets/{ticketId}/attachments/{attachmentId}/download",
            $"api/tickets/{ticketId}/attachments/{attachmentId}/download"
        };

        foreach (var route in routesToTry)
        {
            var res = await client.GetAsync(route);

            if (res.IsSuccessStatusCode)
            {
                var contentType = res.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var bytes = await res.Content.ReadAsByteArrayAsync();

                var fileName = res.Content.Headers.ContentDisposition?.FileNameStar
                               ?? res.Content.Headers.ContentDisposition?.FileName
                               ?? $"evidencia-{attachmentId}";

                fileName = fileName.Trim('"');
                return File(bytes, contentType, fileName);
            }

            if ((int)res.StatusCode == 404 || (int)res.StatusCode == 405)
                continue;

            var bodyBad = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude descargar archivo. Status: {(int)res.StatusCode}. Body: {bodyBad}";
            return RedirectToAction("Details", new { id = ticketId });
        }

        TempData["Error"] = "No pude descargar archivo: no existe endpoint de download para attachment.";
        return RedirectToAction("Details", new { id = ticketId });
    }

    [HttpGet("/Tickets/DownloadEvidence/{evidenceId:guid}")]
    public async Task<IActionResult> DownloadEvidence([FromRoute] Guid evidenceId)
    {
        var client = _http.CreateClient("api");

        var routesToTry = new[]
        {
            $"api/v1/tickets/evidence/{evidenceId}/download",
            $"api/tickets/evidence/{evidenceId}/download"
        };

        foreach (var route in routesToTry)
        {
            var res = await client.GetAsync(route);

            if (res.IsSuccessStatusCode)
            {
                var contentType = res.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var bytes = await res.Content.ReadAsByteArrayAsync();

                var fileName = res.Content.Headers.ContentDisposition?.FileNameStar
                               ?? res.Content.Headers.ContentDisposition?.FileName
                               ?? $"evidence-{evidenceId}";

                fileName = fileName.Trim('"');
                return File(bytes, contentType, fileName);
            }

            if ((int)res.StatusCode == 404 || (int)res.StatusCode == 405)
                continue;

            var bodyBad = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"No pude descargar evidencia. Status: {(int)res.StatusCode}. Body: {bodyBad}";
            return RedirectToAction("Index");
        }

        TempData["Error"] = "No pude descargar evidencia: no existe endpoint.";
        return RedirectToAction("Index");
    }

    // ============================
    // ✅ BORRAR EVIDENCIA (X)
    // ============================

    [HttpDelete("/Tickets/DeleteAttachment/{ticketId:guid}/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment([FromRoute] Guid ticketId, [FromRoute] Guid attachmentId)
    {
        var client = _http.CreateClient("api");

        var routesToTry = new[]
        {
            $"api/v1/tickets/{ticketId}/attachments/{attachmentId}",
            $"api/tickets/{ticketId}/attachments/{attachmentId}",
        };

        foreach (var route in routesToTry.Distinct())
        {
            var res = await client.DeleteAsync(route);

            if (res.IsSuccessStatusCode)
                return Ok(new { ok = true });

            if ((int)res.StatusCode == 404 || (int)res.StatusCode == 405)
                continue;

            var bodyBad = await res.Content.ReadAsStringAsync();
            return StatusCode((int)res.StatusCode, bodyBad);
        }

        return StatusCode(404, "No existe endpoint DELETE para attachments en la API.");
    }

    [HttpDelete("/Tickets/DeleteEvidence/{evidenceId:guid}")]
    public async Task<IActionResult> DeleteEvidence([FromRoute] Guid evidenceId)
    {
        var client = _http.CreateClient("api");

        var routesToTry = new[]
        {
            $"api/v1/tickets/evidence/{evidenceId}",
            $"api/tickets/evidence/{evidenceId}",
        };

        foreach (var route in routesToTry)
        {
            var res = await client.DeleteAsync(route);

            if (res.IsSuccessStatusCode)
                return Ok(new { ok = true });

            if ((int)res.StatusCode == 404 || (int)res.StatusCode == 405)
                continue;

            var bodyBad = await res.Content.ReadAsStringAsync();
            return StatusCode((int)res.StatusCode, bodyBad);
        }

        return StatusCode(404, "No existe endpoint DELETE para evidence en la API.");
    }
}

public record TicketCommentRequest(string By, string Comment);
