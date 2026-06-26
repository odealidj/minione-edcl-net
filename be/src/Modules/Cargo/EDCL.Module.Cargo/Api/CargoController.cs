namespace EDCL.Module.Cargo.Api;

using EDCL.Module.Cargo.Application.Queries.GetManifestParts;
using EDCL.Module.Cargo.Application.Queries.GetManifests;
using EDCL.Shared.Http;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for retrieving cargo manifests, parts, and kanbans data.
/// </summary>
[ApiController]
[Route("api/v1/manifests")]
[Authorize]
public sealed class CargoController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    /// <summary>
    /// Retrieves all manifests associated with a specific route stop.
    /// </summary>
    /// <param name="stopId">The ID of the route stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of manifests for the stop.</returns>
    [HttpGet("stops/{stopId}")]
    [ProducesResponseType(typeof(ApiResponse<ManifestsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetManifests(long stopId, CancellationToken cancellationToken)
    {
        var query = new GetManifestsQuery(stopId);
        var result = await mediator.Send(query, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ManifestsResponse>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    /// <summary>
    /// Retrieves parts aggregation for a specific manifest.
    /// </summary>
    /// <param name="manifestId">The ID of the manifest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregation of parts inside the manifest.</returns>
    [HttpGet("{manifestId}/parts")]
    [ProducesResponseType(typeof(ApiResponse<ManifestPartsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetManifestParts(long manifestId, CancellationToken cancellationToken)
    {
        var query = new GetManifestPartsQuery(manifestId);
        var result = await mediator.Send(query, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ManifestPartsResponse>.Success(ok, traceId)),
            err => NotFound(ApiResponse<object>.Fail(err.Message, traceId, 404))
        );
    }

    /// <summary>
    /// Retrieves paginated kanbans inside a specific manifest.
    /// </summary>
    /// <param name="manifestId">The ID of the manifest.</param>
    /// <param name="pageNumber">Page number for pagination.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of kanbans.</returns>
    [HttpGet("{manifestId}/kanbans")]
    [ProducesResponseType(typeof(ApiResponse<EDCL.Module.Cargo.Application.Queries.GetManifestKanbanDetails.PaginatedResult<EDCL.Module.Cargo.Application.Queries.GetManifestKanbanDetails.ManifestKanbanDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetManifestKanbans(
        long manifestId, 
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new EDCL.Module.Cargo.Application.Queries.GetManifestKanbanDetails.GetManifestKanbanDetailsQuery(manifestId, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<EDCL.Module.Cargo.Application.Queries.GetManifestKanbanDetails.PaginatedResult<EDCL.Module.Cargo.Application.Queries.GetManifestKanbanDetails.ManifestKanbanDto>>.Success(ok, traceId)),
            err => NotFound(ApiResponse<object>.Fail(err.Message, traceId, 404))
        );
    }
}
