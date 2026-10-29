using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
namespace stockFlow.Errors;
public sealed class PersistenceExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        int status; string code; string detail;
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        if (exception is DbUpdateConcurrencyException) { status = 412; code = "stale_version"; detail = "Reload the resource and retry with its current ETag."; }
        else if (sql?.Number is 2601 or 2627) { status = 409; code = "duplicate_resource"; detail = "A resource with this key already exists."; }
        else if (sql?.Number is 547) { status = 409; code = "reference_conflict"; detail = "A reference or database constraint prevents this operation."; }
        else if (sql?.Number is 1205 or 51001 or -2) { status = 503; code = "write_busy"; detail = "The database is busy. Reload the resource before retrying."; context.Response.Headers.RetryAfter = "2"; }
        else if (exception is ArgumentException or OverflowException) { status = 400; code = "invalid_request"; detail = "One or more values violate the documented field limits or decimal policy."; }
        else if (exception is InvalidOperationException && exception.TargetSite?.DeclaringType?.Namespace?.StartsWith("stockFlow.Domain", StringComparison.Ordinal) == true)
        { status = 409; code = "document_state"; detail = "The current document state does not allow this action."; }
        else return false;
        await ApiProblems.WriteAsync(context, ApiProblems.Create(context, status, code, detail), ct); return true;
    }
}
