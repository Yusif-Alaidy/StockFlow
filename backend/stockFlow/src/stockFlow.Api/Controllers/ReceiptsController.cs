using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Authorize, Route("api/v1/receipts")]
public sealed class ReceiptsController(IDocumentService service) : FeatureController
{
    [HttpGet] public Task<PagedResult<DocumentView>> List([FromQuery] PageRequest page, [FromQuery] Guid? warehouseId, [FromQuery] string? status, CancellationToken ct) => service.ListAsync("receipts", page, warehouseId, status, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<DocumentView>> Get(Guid id, CancellationToken ct)
    { var result = await service.GetAsync("receipts", id, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPost] public async Task<ActionResult<DocumentView>> Create(ReceiptInput request, CancellationToken ct)
    { var result = await service.ReceiveAsync(request, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPost("{id:guid}/post")]
    public async Task<ActionResult<DocumentView>> Post(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("receipts", id, "post", Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<DocumentView>> Cancel(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("receipts", id, "cancel", Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin + "," + Roles.PurchasingOfficer), HttpPost("{id:guid}/reopen")]
    public async Task<ActionResult<DocumentView>> Reopen(Guid id, CancellationToken ct)
    { var result = await service.ActAsync("receipts", id, "reopen", Version(), ct); return Versioned(result, result.Version); }
}
