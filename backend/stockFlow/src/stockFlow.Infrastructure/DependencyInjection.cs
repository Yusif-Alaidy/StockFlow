using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using stockFlow.Infrastructure.Persistence;

namespace stockFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<StockFlowDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<DocumentNumberGenerator>();
        return services;
    }
}
