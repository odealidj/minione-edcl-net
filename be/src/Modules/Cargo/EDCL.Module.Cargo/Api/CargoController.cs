namespace EDCL.Module.Cargo.Api;

using EDCL.Module.Cargo.Application.Queries.GetManifestParts;
using EDCL.Module.Cargo.Application.Queries.GetManifests;
using EDCL.Shared.Http;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/manifests")]
[Authorize]
public sealed class CargoController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("stops/{stopId}")]
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

    [HttpGet("{manifestId}/parts")]
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

    [HttpGet("{manifestId}/kanbans")]
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
