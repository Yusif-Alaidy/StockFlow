using Microsoft.AspNetCore.RateLimiting;
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
        var bootstrapAdmin = args.Contains("--bootstrap-admin", StringComparer.Ordinal);
        // Custom flag is not a key/value configuration switch; remove it before host parsing.
        var builder = WebApplication.CreateBuilder(args.Where(a => a != "--bootstrap-admin").ToArray());

        builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<stockFlow.Application.Abstractions.ICurrentUser, stockFlow.Security.CurrentUser>();
        builder.Services.AddAuthentication("Bearer").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, stockFlow.Security.BearerHandler>("Bearer", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = 429;
            o.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await ApiProblems.WriteAsync(context.HttpContext, ApiProblems.Create(context.HttpContext, 429, "too_many_requests", "Too many authentication attempts. Try again later."), ct);
            };
            o.AddPolicy("auth", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        builder.Services.AddTransient<ProblemDetailsFactory, ApiProblemDetailsFactory>();
        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context => ApiProblems.Enrich(context.HttpContext, context.ProblemDetails));
        builder.Services.AddExceptionHandler<PersistenceExceptionHandler>();
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

        builder.Services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<stockFlow.OpenApi.ContractTransformer>();
            options.AddSchemaTransformer((schema, context, ct) =>
            {
                if (context.JsonTypeInfo.Type == typeof(decimal)) schema.Format = "decimal";
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Info.Title = "StockFlow API";
            document.Info.Version = "v1";
            document.Info.Description = "Inventory API. Health routes are unversioned; business routes use /api/v1.";
            document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>
            {
                ["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme { Type = Microsoft.OpenApi.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "Opaque session token" }
            };
            return Task.CompletedTask;
        });
        });

        var app = builder.Build();
        // Explicit command, never automatic migrations/seeding on web startup.
        if (bootstrapAdmin)
        {
            using var scope = app.Services.CreateScope();
            var email = Environment.GetEnvironmentVariable("STOCKFLOW_BOOTSTRAP_EMAIL") ?? throw new InvalidOperationException("Set STOCKFLOW_BOOTSTRAP_EMAIL.");
            var password = Environment.GetEnvironmentVariable("STOCKFLOW_BOOTSTRAP_PASSWORD") ?? throw new InvalidOperationException("Set STOCKFLOW_BOOTSTRAP_PASSWORD.");
            scope.ServiceProvider.GetRequiredService<stockFlow.Application.Features.IAuthService>().BootstrapAsync(email, password, CancellationToken.None).GetAwaiter().GetResult();
            return;
        }

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
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers.CacheControl = "no-store";
            await next();
        });

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
