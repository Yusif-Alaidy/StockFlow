using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using stockFlow.Errors;

namespace stockFlow.Controllers;

public sealed record HealthResponse(string Status);

[ApiController]
[Route("health")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _health;
    public HealthController(HealthCheckService health) => _health = health;

    [HttpGet("live")]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Live() => Ok(new HealthResponse("Healthy"));

    [HttpGet("ready")]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<HealthResponse>> Ready(CancellationToken ct)
    {
        var report = await _health.CheckHealthAsync(check => check.Tags.Contains("ready"), ct);
        if (report.Status == HealthStatus.Healthy) return Ok(new HealthResponse("Healthy"));
        var result = new ObjectResult(ApiProblems.Create(HttpContext, 503)) { StatusCode = 503 };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
