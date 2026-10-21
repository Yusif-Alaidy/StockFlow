using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using stockFlow.Infrastructure.Persistence;

namespace stockFlow.IntegrationTests.Persistence;

// A new disposable database per test; never uses or deletes the configured catalog.
internal sealed class SqlTestDatabase : IAsyncDisposable
{
    private readonly string _databaseName = "StockFlowTests_" + Guid.NewGuid().ToString("N");
    private DbContextOptions<StockFlowDbContext>? _options;
    private string? _connectionString;

    public StockFlowDbContext CreateContext() => new(_options
        ?? throw new InvalidOperationException("Initialize test database first."));

    public async Task InitializeAsync()
    {
        var value = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_SQL");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Set STOCKFLOW_TEST_SQL to a LOCAL SQL Server connection. See README-AR.md.");
        var builder = new SqlConnectionStringBuilder(value);
        var server = builder.DataSource.ToLowerInvariant();
        var local = server == "." || server == "(local)" || server == "localhost" || server == "127.0.0.1"
            || server.StartsWith("(localdb)\\", StringComparison.Ordinal)
            || server.StartsWith(".\\", StringComparison.Ordinal)
            || server.StartsWith("localhost,", StringComparison.Ordinal)
            || server.StartsWith("127.0.0.1,", StringComparison.Ordinal)
            || server.StartsWith("localhost\\", StringComparison.Ordinal);
        if (!local) throw new InvalidOperationException("Tests allow local SQL Server only. Hosted databases are forbidden.");
        builder.InitialCatalog = _databaseName;
        builder.AttachDBFilename = "";
        builder.Pooling = false;
        builder.ConnectTimeout = 15;
        _connectionString = builder.ConnectionString;
        _options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseSqlServer(_connectionString).Options;
        await using var context = CreateContext();
        if (!context.Database.GetMigrations().Any())
            throw new InvalidOperationException("No migrations found. Generate and review the new migration first.");
        await context.Database.MigrateAsync();
        if (!await context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE [name] = 'StockBalances'").AnyAsync(x => x > 0))
            throw new InvalidOperationException("StockBalances is missing. Generate the Phase 04 migration.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_options is null || _connectionString is null) return;
        var builder = new SqlConnectionStringBuilder(_connectionString);
        if (builder.InitialCatalog != _databaseName || !_databaseName.StartsWith("StockFlowTests_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to delete a database not created by this fixture.");
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
