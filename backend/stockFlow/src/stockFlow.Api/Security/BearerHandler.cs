using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using stockFlow.Application.Features;
namespace stockFlow.Security;
public sealed class BearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IAuthService auth) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header)) return AuthenticateResult.NoResult();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || header.Length > 200) return AuthenticateResult.Fail("Invalid bearer token.");
        var principal = await auth.ValidateAsync(header[7..], Context.RequestAborted);
        if (principal is null) return AuthenticateResult.Fail("Invalid or expired bearer token.");
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, principal.UserId.ToString()),
            new Claim(ClaimTypes.Role, principal.Role), new Claim("session_id", principal.SessionId.ToString())], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
