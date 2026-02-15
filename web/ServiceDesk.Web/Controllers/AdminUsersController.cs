using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ServiceDesk.Web.Controllers;

public class AdminUsersController : Controller
{
    private readonly IHttpClientFactory _http;

    public AdminUsersController(IHttpClientFactory http)
    {
        _http = http;
    }

    private HttpClient Api()
    {
        var client = _http.CreateClient("api");

        var token = HttpContext.Session.GetString("access_token");
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    public async Task<IActionResult> Index()
    {
        var res = await Api().GetAsync("api/admin/users");
        if (!res.IsSuccessStatusCode)
        {
            TempData["Error"] = $"No se pudo cargar usuarios ({(int)res.StatusCode}).";
            return View(new List<UserItemVm>());
        }

        var json = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var items = doc.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(x => new UserItemVm
            {
                Id = x.GetProperty("id").GetGuid(),
                Username = x.GetProperty("username").GetString() ?? "",
                DisplayName = x.GetProperty("displayName").GetString() ?? "",
                Email = x.TryGetProperty("email", out var e) ? e.GetString() : null,
                IsActive = x.GetProperty("isActive").GetBoolean()
            })
            .ToList();

        return View(items);
    }

    public IActionResult Create() => View(new CreateUserVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var payload = JsonSerializer.Serialize(new
        {
            username = vm.Username,
            displayName = vm.DisplayName,
            password = vm.Password,
            email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email,
            isActive = vm.IsActive
        });

        var res = await Api().PostAsync("api/admin/users",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error creando usuario: {err}";
            return View(vm);
        }

        TempData["Success"] = "Usuario creado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var res = await Api().GetAsync("api/admin/users");
        if (!res.IsSuccessStatusCode)
        {
            TempData["Error"] = $"No se pudo cargar usuarios ({(int)res.StatusCode}).";
            return RedirectToAction(nameof(Index));
        }

        var json = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var u = doc.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(x => new UserItemVm
            {
                Id = x.GetProperty("id").GetGuid(),
                Username = x.GetProperty("username").GetString() ?? "",
                DisplayName = x.GetProperty("displayName").GetString() ?? "",
                Email = x.TryGetProperty("email", out var e) ? e.GetString() : null,
                IsActive = x.GetProperty("isActive").GetBoolean()
            })
            .FirstOrDefault(x => x.Id == id);

        if (u == null) return NotFound();

        return View(new EditUserVm
        {
            Id = u.Id,
            Username = u.Username,
            DisplayName = u.DisplayName,
            Email = u.Email,
            IsActive = u.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var payload = JsonSerializer.Serialize(new
        {
            displayName = vm.DisplayName,
            isActive = vm.IsActive,
            username = string.IsNullOrWhiteSpace(vm.Username) ? null : vm.Username,
            email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email,
            newPassword = string.IsNullOrWhiteSpace(vm.NewPassword) ? null : vm.NewPassword
        });

        var res = await Api().PutAsync($"api/admin/users/{vm.Id}",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error actualizando usuario: {err}";
            return View(vm);
        }

        TempData["Success"] = "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var res = await Api().DeleteAsync($"api/admin/users/{id}");
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error borrando usuario: {err}";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Usuario eliminado.";
        return RedirectToAction(nameof(Index));
    }
}

public class UserItemVm
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUserVm
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}

public class EditUserVm
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public string? NewPassword { get; set; }
}
