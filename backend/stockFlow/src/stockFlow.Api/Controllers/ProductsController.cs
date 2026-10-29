using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Authorize, Route("api/v1/products")]
public sealed class ProductsController(IMasterService service) : FeatureController
{
    [HttpGet] public Task<PagedResult<MasterView>> List([FromQuery] PageRequest page, [FromQuery] string? search, [FromQuery] bool? active, CancellationToken ct) => service.ListAsync("products", page, search, active, ct);
    [HttpGet("{id:guid}")] public async Task<ActionResult<MasterView>> Get(Guid id, CancellationToken ct)
    { var result = await service.GetAsync("products", id, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin), HttpPost] public async Task<ActionResult<MasterView>> Create(MasterInput request, CancellationToken ct)
    { var result = await service.CreateAsync("products", request, ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin), HttpPut("{id:guid}")] public async Task<ActionResult<MasterView>> Update(Guid id, MasterInput request, CancellationToken ct)
    { var result = await service.UpdateAsync("products", id, request, Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin), HttpPost("{id:guid}/activate")] public async Task<ActionResult<MasterView>> Activate(Guid id, CancellationToken ct)
    { var result = await service.SetActiveAsync("products", id, true, Version(), ct); return Versioned(result, result.Version); }
    [Authorize(Roles = Roles.Admin), HttpPost("{id:guid}/deactivate")] public async Task<ActionResult<MasterView>> Deactivate(Guid id, CancellationToken ct)
    { var result = await service.SetActiveAsync("products", id, false, Version(), ct); return Versioned(result, result.Version); }
}
