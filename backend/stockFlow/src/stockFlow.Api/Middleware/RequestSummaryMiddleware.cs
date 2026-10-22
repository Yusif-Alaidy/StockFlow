using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace stockFlow.Middleware;

public sealed class RequestSummaryMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSummaryMiddleware> _logger;
    public RequestSummaryMiddleware(RequestDelegate next, ILogger<RequestSummaryMiddleware> logger)
    {
        _next = next; _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try { await _next(context); }
        finally
        {
            var template = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            _logger.LogInformation("HTTP {Method} {RouteTemplate} returned {StatusCode} in {ElapsedMs}ms; trace {TraceId}",
                context.Request.Method, template, context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                Activity.Current?.Id ?? context.TraceIdentifier);
        }
    }
}
