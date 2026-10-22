using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace stockFlow.Errors;

public sealed class ApiProblemDetailsFactory : ProblemDetailsFactory
{
    public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null,
        string? title = null, string? type = null, string? detail = null, string? instance = null)
    {
        var result = ApiProblems.Create(httpContext, statusCode ?? 500, detail: detail);
        if (title is not null) result.Title = title;
        if (type is not null) result.Type = type;
        if (instance is not null) result.Instance = instance;
        return result;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
        ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null,
        string? type = null, string? detail = null, string? instance = null)
    {
        var result = new ValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode ?? 400, Title = title ?? "Validation failed",
            Type = type ?? "about:blank", Detail = detail,
            Instance = instance ?? httpContext.Request.Path
        };
        ApiProblems.Enrich(httpContext, result, "validation_failed");
        return result;
    }
}
