using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace stockFlow.Errors;

internal static class ApiProblems
{
    public static ProblemDetails Create(HttpContext context, int status, string? code = null, string? detail = null)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch
            {
                400 => "Bad request", 401 => "Authentication required", 403 => "Access denied",
                404 => "Resource not found", 405 => "Method not allowed", 409 => "Conflict",
                412 => "Precondition failed", 428 => "Precondition required",
                503 => "Service unavailable", _ => "An unexpected error occurred"
            },
            Type = "about:blank",
            Detail = detail,
            Instance = context.Request.Path
        };
        Enrich(context, problem, code);
        return problem;
    }

    public static void Enrich(HttpContext context, ProblemDetails problem, string? code = null)
    {
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problem.Extensions["code"] = code ?? (problem.Status switch
        {
            400 => "bad_request", 401 => "unauthorized", 403 => "forbidden", 404 => "not_found",
            405 => "method_not_allowed", 409 => "conflict", 412 => "precondition_failed",
            428 => "precondition_required", 503 => "service_unavailable", _ => "server_error"
        });
    }

    public static Task WriteAsync(HttpContext context, ProblemDetails problem, CancellationToken ct = default)
    {
        context.Response.StatusCode = problem.Status ?? 500;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(problem,
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json", cancellationToken: ct);
    }
}
