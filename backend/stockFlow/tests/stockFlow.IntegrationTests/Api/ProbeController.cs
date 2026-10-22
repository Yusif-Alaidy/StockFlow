using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using stockFlow.Application.Common.Errors;
using stockFlow.Http;

namespace stockFlow.IntegrationTests.Api;

// TEST PROJECT ONLY. Never add this controller to stockFlow.Api.
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("__tests")]
public sealed class ProbeController : ControllerBase
{
    public sealed class Input
    {
        [Required]
        public string? Name { get; init; }
    }
    [HttpPost("validation")]
    public IActionResult Validate([FromBody] Input input) => Ok();
    [HttpGet("missing")]
    public IActionResult Missing() => NotFound();
    [HttpGet("forbidden")]
    public IActionResult Forbidden() => StatusCode(403);
    [HttpGet("unauthorized")]
    public IActionResult UnauthorizedProbe() => Unauthorized();
    [HttpGet("conflict")]
    public IActionResult ConflictProbe() => throw new AppException(ErrorKind.Conflict, "document_conflict", "The document cannot be changed.");
    [HttpGet("error")]
    public IActionResult Error() => throw new InvalidOperationException("SECRET_CONNECTION_PASSWORD");
    [HttpGet("etag")]
    public IActionResult Etag()
    {
        EntityTag.RequireMatches(Request, new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });
        return Ok();
    }
}
