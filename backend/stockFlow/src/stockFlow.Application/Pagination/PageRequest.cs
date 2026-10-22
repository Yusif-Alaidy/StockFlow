using System.Collections.Generic;
using stockFlow.Application.Common.Errors;

namespace stockFlow.Application.Common.Pagination;

public sealed class PageRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public int GetSkip()
    {
        var errors = new Dictionary<string, string[]>();
        if (PageNumber < 1) errors["pageNumber"] = new[] { "Page number must be at least 1." };
        if (PageSize < 1 || PageSize > 100) errors["pageSize"] = new[] { "Page size must be between 1 and 100." };
        var skip = ((long)PageNumber - 1) * PageSize;
        if (skip > int.MaxValue) errors["pageNumber"] = new[] { "Requested page is too large." };
        if (errors.Count > 0) throw new AppException(ErrorKind.Validation, "validation_failed", "Validation failed.", errors);
        return (int)skip;
    }
}
