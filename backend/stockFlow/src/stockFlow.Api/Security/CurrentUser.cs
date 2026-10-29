using System.Security.Claims;
using stockFlow.Application.Abstractions;
namespace stockFlow.Security;
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
