using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Authorize, Route("api/v1/issues")]
public sealed class IssuesController(IDocumentService service) : FeatureController
{
    [HttpGet] public Task<PagedResult<DocumentView>> List([FromQuery] PageRequest page, [FromQuery] Guid? warehouseId, [FromQuery] string? status, CancellationToken ct) => service.ListAsync("issues", page, warehouseId, status, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<DocumentView>> Get(Guid id, CancellationToken ct)
    { var result = await service.GetAsync("issues", id, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPost] public async Task<ActionResult<DocumentView>> Create(DocumentInput request, CancellationToken ct)
    { var result = await service.CreateAsync("issues", request, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentView>> Edit(Guid id, DocumentEdit request, CancellationToken ct)
    { var result = await service.EditAsync("issues", id, request, Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPost("{id:guid}/post")]
    public async Task<ActionResult<DocumentView>> Post(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("issues", id, "post", Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseKeeper), HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<DocumentView>> Cancel(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("issues", id, "cancel", Version(), ct); return Versioned(result, result.Version); }
}
