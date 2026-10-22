using Microsoft.AspNetCore.Diagnostics;
using stockFlow.Application.Common.Errors;

namespace stockFlow.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
            return true; // Client disconnected; do not attempt to write a response body.
        }
        if (exception is AppException appError)
        {
            var status = appError.Kind switch
            {
                ErrorKind.Validation => 400, ErrorKind.Unauthorized => 401,
                ErrorKind.Forbidden => 403, ErrorKind.NotFound => 404, ErrorKind.Conflict => 409,
                ErrorKind.PreconditionRequired => 428, ErrorKind.PreconditionFailed => 412, _ => 500
            };
            var problem = ApiProblems.Create(context, status, appError.Code, appError.Message);
            if (appError.Errors is not null) problem.Extensions["errors"] = appError.Errors;
            await ApiProblems.WriteAsync(context, problem, ct);
            return true;
        }
        // Do not log exception.Message/ToString: SQL or upstream messages may contain secrets.
        _logger.LogError("Unhandled exception {ExceptionType}; trace {TraceId}",
            exception.GetType().FullName, System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier);
        await ApiProblems.WriteAsync(context, ApiProblems.Create(context, 500), ct);
        return true;
    }
}
