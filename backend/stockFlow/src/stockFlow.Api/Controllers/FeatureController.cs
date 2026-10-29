using Microsoft.AspNetCore.Mvc;
namespace stockFlow.Controllers;
[ApiController]
[Produces("application/json")]
public abstract class FeatureController : ControllerBase
{
    protected ActionResult<T> Versioned<T>(T result, string version)
    {
        Response.Headers.ETag = "\"" + version + "\"";
        return Ok(result);
    }
    protected byte[] Version() => stockFlow.Http.EntityTag.RequireIfMatch(Request);
}
