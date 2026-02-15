// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Controllers\AdminMaintenanceController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/maintenance")]
[Authorize(Roles = "Admin")]
public class AdminMaintenanceController : ControllerBase
{
    // GET: api/admin/maintenance/ping
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            ok = true,
            utc = DateTime.UtcNow
        });
    }
}
