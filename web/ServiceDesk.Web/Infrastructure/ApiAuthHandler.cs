using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace ServiceDesk.Web.Infrastructure;

public class ApiAuthHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;

    public ApiAuthHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var http = _ctx.HttpContext;

        var token = http?.Session.GetString("access_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var res = await base.SendAsync(request, cancellationToken);

        if (res.StatusCode == HttpStatusCode.Unauthorized && http is not null)
        {
            http.Session.Remove("access_token");
            http.Session.Remove("roles");
            http.Session.Remove("display_name");
        }

        return res;
    }
}
