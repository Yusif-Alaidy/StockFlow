using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using stockFlow.Application;
using stockFlow.Errors;
using stockFlow.Health;
using stockFlow.Infrastructure;
using stockFlow.Infrastructure.Persistence;
using stockFlow.Middleware;

namespace stockFlow;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddTransient<ProblemDetailsFactory, ApiProblemDetailsFactory>();
        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context => ApiProblems.Enrich(context.HttpContext, context.ProblemDetails));
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddApplication();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        builder.Services.AddInfrastructure(connectionString);
        builder.Services.AddScoped<DatabaseAvailability>();
        builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database",
            tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5));

        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(options => options.AddPolicy("Angular", policy =>
        {
            if (origins.Length > 0) policy.WithOrigins(origins);
            policy.WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Authorization", "If-Match")
                .WithExposedHeaders("ETag");
        }));

        builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Info.Title = "StockFlow API";
            document.Info.Version = "v1";
            document.Info.Description = "Inventory API. Health routes are unversioned; business routes use /api/v1.";
            return Task.CompletedTask;
        }));

        var app = builder.Build();

        // Keep the summary outside the handler so the logged status includes handled failures.
        app.UseMiddleware<RequestSummaryMiddleware>();
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            // ApiExceptionHandler records a safe exception type + trace ID instead of raw messages.
            SuppressDiagnosticsCallback = _ => true
        });
        app.UseStatusCodePages(async statusContext =>
        {
            var context = statusContext.HttpContext;
            await ApiProblems.WriteAsync(context, ApiProblems.Create(context, context.Response.StatusCode), context.RequestAborted);
        });
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("Angular");
        app.UseAuthorization();

        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Api:ExposeDocumentation"))
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "docs";
                // Relative URL respects a hosting PathBase, if configured by the host.
                options.SwaggerEndpoint("../openapi/v1.json", "StockFlow API v1");
                options.DisplayRequestDuration();
            });
        }
        app.MapControllers();
        app.Run();
    }
}
