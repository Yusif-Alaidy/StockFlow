using System.Collections.Generic;

namespace stockFlow.Application.Common.Pagination;

// The use case validates PageRequest and fills this response after Count/Skip/Take.
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items, int PageNumber, int PageSize, long TotalCount);
