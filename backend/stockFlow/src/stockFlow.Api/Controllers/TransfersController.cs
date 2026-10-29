using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Authorize, Route("api/v1/transfers")]
public sealed class TransfersController(IDocumentService service) : FeatureController
{
    [HttpGet] public Task<PagedResult<DocumentView>> List([FromQuery] PageRequest page, [FromQuery] Guid? warehouseId, [FromQuery] string? status, CancellationToken ct) => service.ListAsync("transfers", page, warehouseId, status, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<DocumentView>> Get(Guid id, CancellationToken ct)
    { var result = await service.GetAsync("transfers", id, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPost] public async Task<ActionResult<DocumentView>> Create(DocumentInput request, CancellationToken ct)
    { var result = await service.CreateAsync("transfers", request, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentView>> Edit(Guid id, DocumentEdit request, CancellationToken ct)
    { var result = await service.EditAsync("transfers", id, request, Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPost("{id:guid}/post")]
    public async Task<ActionResult<DocumentView>> Post(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("transfers", id, "post", Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<DocumentView>> Cancel(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("transfers", id, "cancel", Version(), ct); return Versioned(result, result.Version); }
}
