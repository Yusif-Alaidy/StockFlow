using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace stockFlow.Infrastructure.Persistence;

// SQL sequence is shared by all document types. Gaps are normal and allowed.
// Each concurrent request must have its own scoped DbContext.
public sealed class DocumentNumberGenerator
{
    private readonly StockFlowDbContext _context;
    public DocumentNumberGenerator(StockFlowDbContext context) => _context = context;

    public Task<string> NextPurchaseOrderAsync(CancellationToken ct = default) => NextAsync("PO", ct);
    public Task<string> NextOpeningAsync(CancellationToken ct = default) => NextAsync("OP", ct);
    public Task<string> NextReceiptAsync(CancellationToken ct = default) => NextAsync("RC", ct);
    public Task<string> NextIssueAsync(CancellationToken ct = default) => NextAsync("IS", ct);
    public Task<string> NextTransferAsync(CancellationToken ct = default) => NextAsync("TR", ct);

    private async Task<string> NextAsync(string prefix, CancellationToken ct)
    {
        // Materialize before reading the value: NEXT VALUE FOR must not be composed into a subquery.
        var values = await _context.Database.SqlQueryRaw<long>(
            "SELECT NEXT VALUE FOR [dbo].[DocumentNumbers] AS [Value]").ToListAsync(ct);
        return prefix + "-" + values[0].ToString("D10", CultureInfo.InvariantCulture);
    }
}
