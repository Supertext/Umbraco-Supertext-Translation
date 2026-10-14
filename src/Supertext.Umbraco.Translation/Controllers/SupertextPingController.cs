using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supertext.Umbraco.Translation.Services;

namespace Supertext.Umbraco.Translation.Controllers;

/// <summary>
/// Public ping route for the Supertext Commando Center: <c>GET /umbraco/supertext/ping?challenge=…</c>.
/// No login, no session, no antiforgery token; the answer (status, headers, body) is exactly the SDK's
/// <c>PingResponse</c>: 200 with the challenge echoed, 400 for a bad challenge, 404 while version reports are off.
/// </summary>
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SupertextPingController(SupertextReporting reporting) : ControllerBase
{
    [HttpGet("/" + SupertextReporting.PingPath)]
    public IActionResult Ping([FromQuery] string? challenge)
    {
        var answer = reporting.Ping(challenge);
        foreach (var (name, value) in answer.Headers)
        {
            Response.Headers[name] = value;
        }
        return new ContentResult
        {
            StatusCode = answer.Status,
            Content = answer.Body,
            ContentType = answer.Headers.TryGetValue("Content-Type", out var type) ? type : "application/json",
        };
    }
}
