using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
namespace stockFlow.Infrastructure.Persistence;
public sealed class StockFlowDesignTimeFactory : IDesignTimeDbContextFactory<StockFlowDbContext>
{
    public StockFlowDbContext CreateDbContext(string[] args)
    {
        // Same non-secret identifier as the API project. Environment overrides User Secrets.
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("e0d0362c-885f-4e49-aca9-983485c7e6c0")
            .AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection");
        var options = new DbContextOptionsBuilder<StockFlowDbContext>();
        if (string.IsNullOrWhiteSpace(connection))
            // Model/script generation needs only the provider. Database commands fail without a connection.
            options.UseSqlServer();
        else options.UseSqlServer(connection);
        return new StockFlowDbContext(options.Options);
    }
}
