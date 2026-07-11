using EDCL.Module.Cargo.Application.Queries.GetManifestDetail;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Cargo.Api;

/// <summary>
/// Controller for managing Mobile Ingestion Cargo data (Master Manifests).
/// </summary>
[ApiController]
[Route("api/v1/mobile/cargo/manifests")]
[Tags("Cargo Ingestion (Mobile)")]
[Authorize]
public class MobileCargoController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Gets the detailed part list of a specific manifest.
    /// </summary>
    /// <param name="manifestNo">The manifest number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed manifest information including parts</returns>
    [HttpGet("{manifestNo}/detail")]
    [ProducesResponseType(typeof(ApiResponse<ManifestDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetManifestDetail(string manifestNo, CancellationToken cancellationToken)
    {
        var query = new GetManifestDetailQuery(manifestNo);
        var result = await mediator.Send(query, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ManifestDetailDto>.Success(ok, traceId)),
            err => NotFound(ApiResponse<object>.Fail(err.Message, traceId, 404))
        );
    }
}
