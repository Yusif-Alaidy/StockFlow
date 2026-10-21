using System;
using System.Threading;
using System.Threading.Tasks;

namespace stockFlow.Application.Abstractions;

public interface IWarehouseAccess
{
    Task<bool> CanAccessAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
