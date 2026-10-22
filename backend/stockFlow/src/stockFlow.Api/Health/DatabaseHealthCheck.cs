using Microsoft.Extensions.Diagnostics.HealthChecks;
using stockFlow.Infrastructure.Persistence;

namespace stockFlow.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopes;
    public DatabaseHealthCheck(IServiceScopeFactory scopes) => _scopes = scopes;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var available = await scope.ServiceProvider.GetRequiredService<DatabaseAvailability>().CanConnectAsync(ct);
            return available ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database unavailable.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Database check timed out or was cancelled.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Database unavailable.");
        }
    }
}
