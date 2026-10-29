using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Pagination;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Authorize(Roles = Roles.Admin), Route("api/v1/users")]
public sealed class UsersController(IUserService users) : FeatureController
{
    [HttpGet] public Task<PagedResult<UserView>> List([FromQuery] PageRequest page, CancellationToken ct) => users.ListAsync(page, ct);
    [HttpPost] public async Task<ActionResult<UserView>> Create(CreateUserRequest request, CancellationToken ct)
    { var result = await users.CreateAsync(request, ct); return Versioned(result, result.Version); }
    [HttpPut("{id:guid}")] public async Task<ActionResult<UserView>> Update(Guid id, UpdateUserRequest request, CancellationToken ct)
    { var result = await users.UpdateAsync(id, request, Version(), ct); return Versioned(result, result.Version); }
}
