using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Authorize, Route("api/v1/purchase-orders")]
public sealed class PurchaseOrdersController(IDocumentService service) : FeatureController
{
    [HttpGet] public Task<PagedResult<DocumentView>> List([FromQuery] PageRequest page, [FromQuery] Guid? warehouseId, [FromQuery] string? status, CancellationToken ct) => service.ListAsync("purchase-orders", page, warehouseId, status, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<DocumentView>> Get(Guid id, CancellationToken ct)
    { var result = await service.GetAsync("purchase-orders", id, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPost] public async Task<ActionResult<DocumentView>> Create(DocumentInput request, CancellationToken ct)
    { var result = await service.CreateAsync("purchase-orders", request, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentView>> Edit(Guid id, DocumentEdit request, CancellationToken ct)
    { var result = await service.EditAsync("purchase-orders", id, request, Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<DocumentView>> Submit(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("purchase-orders", id, "submit", Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<DocumentView>> Cancel(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("purchase-orders", id, "cancel", Version(), ct); return Versioned(result, result.Version); }
}
