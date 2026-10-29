using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using stockFlow.Application.Features;
namespace stockFlow.Controllers;
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService auth) : FeatureController
{
    [AllowAnonymous, HttpPost("login"), EnableRateLimiting("auth")]
    public async Task<ActionResult<TokenResult>> Login(LoginRequest request, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return Ok(await auth.LoginAsync(request, ct)); }
    [AllowAnonymous, HttpPost("refresh"), EnableRateLimiting("auth")]
    public async Task<ActionResult<TokenResult>> Refresh(RefreshRequest request, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return Ok(await auth.RefreshAsync(request.RefreshToken, ct)); }
    [Authorize, HttpGet("me")]
    public async Task<ActionResult<UserView>> Me(CancellationToken ct)
    { var result = await auth.MeAsync(ct); return Versioned(result, result.Version); }
    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    { await auth.LogoutAsync(Guid.Parse(User.FindFirstValue("session_id")!), ct); return NoContent(); }
    [Authorize, HttpPost("change-password"), EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    { await auth.ChangePasswordAsync(request, ct); return NoContent(); }
}
