using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace stockFlow.Infrastructure.Persistence;

public sealed class DatabaseAvailability
{
    private readonly StockFlowDbContext _context;
    public DatabaseAvailability(StockFlowDbContext context) => _context = context;

    // Connectivity check only; does not apply migrations or change inventory.
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        _context.Database.CanConnectAsync(cancellationToken);
}
