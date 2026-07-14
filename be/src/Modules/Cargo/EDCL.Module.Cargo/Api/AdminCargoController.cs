using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Cargo.Api;

[ApiController]
[Route("api/v1/web/master/cargo")]
[Tags("Admin Master Data (Cargo)")]
[Authorize(Roles = "ADMIN")]
public class AdminCargoController(IMediator mediator) : ControllerBase
{
    [HttpGet("manifests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManifests([FromQuery] string? search = null, [FromQuery] string? supplierCode = null, [FromQuery] string? status = null, [FromQuery] bool? isAssignedToRoute = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.AdminGetManifests.AdminGetManifestsQuery(search, supplierCode, status, isAssignedToRoute, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("manifests/pending-suppliers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Application.Queries.GetPendingManifestSuppliers.PendingManifestSupplierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingManifestSuppliers(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.GetPendingManifestSuppliers.GetPendingManifestSuppliersQuery(), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<Application.Queries.GetPendingManifestSuppliers.PendingManifestSupplierDto>>.Success(result.Value, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("manifest-parts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestPartDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManifestParts([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.AdminGetManifestParts.AdminGetManifestPartsQuery(search, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestPartDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("manifest-kanbans")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestKanbanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManifestKanbans([FromQuery] long? manifestId = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.AdminGetManifestKanbans.AdminGetManifestKanbansQuery(manifestId, search, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestKanbanDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("manifest-problems")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestProblemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetManifestProblems([FromQuery] string? search = null, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.AdminGetManifestProblems.AdminGetManifestProblemsQuery(search, status, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<Application.DTOs.AdminManifestProblemDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpPatch("manifest-problems/{id}/resolve")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveManifestProblem(long id, [FromBody] ResolveManifestProblemRequest request, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Commands.ResolveManifestProblem.ResolveManifestProblemCommand(id, request.Reason), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<bool>.Success(true, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }
}

public sealed record ResolveManifestProblemRequest(string Reason);
