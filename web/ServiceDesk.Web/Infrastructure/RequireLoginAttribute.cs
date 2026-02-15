using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ServiceDesk.Web.Infrastructure;

public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var req = context.HttpContext.Request;
        var path = (req.Path.Value ?? "").ToLowerInvariant();

        // ✅ Permitir Auth completo
        if (path.StartsWith("/auth"))
            return;

        // ✅ Permitir estáticos
        if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib") ||
            path.StartsWith("/images") || path.StartsWith("/favicon"))
            return;

        // ✅ Respetar AllowAnonymous
        var allowAnon = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (allowAnon) return;

        var token = context.HttpContext.Session.GetString("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            // ✅ returnUrl REAL (incluye querystring)
            var returnUrl = req.Path + req.QueryString;
            context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl = returnUrl.ToString() });
        }
    }
}
