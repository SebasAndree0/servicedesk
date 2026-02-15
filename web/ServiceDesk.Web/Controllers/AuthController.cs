// C:\ProyectoASPNET\servicedesk\web\ServiceDesk.Web\Controllers\AuthController.cs

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceDesk.Web.Controllers;

public class AuthController : Controller
{
    private readonly IHttpClientFactory _http;

    public AuthController(IHttpClientFactory http) => _http = http;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return View();
    }

    // DTOs para conversar con la API
    public record LoginRequest(string UsernameOrEmail, string Password);

    public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, UserDto User);
    public record UserDto(Guid Id, string Username, string Email, string DisplayName, string[] Roles);

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string usernameOrEmail, string password, string? returnUrl = null)
    {
        var client = _http.CreateClient("api");

        var res = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(usernameOrEmail, password));

        if (!res.IsSuccessStatusCode)
        {
            ViewBag.Error = "Credenciales inválidas.";
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View();
        }

        var data = await res.Content.ReadFromJsonAsync<LoginResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (data is null || string.IsNullOrWhiteSpace(data.AccessToken))
        {
            ViewBag.Error = "No pude leer la respuesta del login.";
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View();
        }

        // ✅ Guardar en Session (lo PRO)
        HttpContext.Session.SetString("access_token", data.AccessToken);

        var roles = data.User?.Roles ?? Array.Empty<string>();
        HttpContext.Session.SetString("roles", string.Join(",", roles));

        // ✅ Guardar datos de usuario (PRO para auditoría / CreatedBy / AssignedTo)
        if (data.User is not null)
        {
            HttpContext.Session.SetString("user_id", data.User.Id.ToString());
            HttpContext.Session.SetString("username", data.User.Username ?? "");
            HttpContext.Session.SetString("email", data.User.Email ?? "");
        }
        else
        {
            HttpContext.Session.Remove("user_id");
            HttpContext.Session.Remove("username");
            HttpContext.Session.Remove("email");
        }

        // ✅ DisplayName (lo que muestras arriba a la derecha)
        var display = data.User?.DisplayName;
        if (string.IsNullOrWhiteSpace(display))
            display = data.User?.Email ?? data.User?.Username ?? "Usuario";

        HttpContext.Session.SetString("display_name", display);

        return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
