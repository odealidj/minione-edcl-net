using EDCL.Module.Job.Application.Commands.AdminCreatePickupOrder;
using EDCL.Module.Job.Application.Commands.AdminDeletePickupOrder;
using EDCL.Module.Job.Application.Commands.AdminUpdatePickupOrder;
using EDCL.Module.Job.Application.Queries.AdminGetPickupOrderById;
using EDCL.Module.Job.Application.Queries.AdminGetPickupOrders;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Api;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/v1/web/admin/pickup-orders")]
public class AdminPickupOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] AdminGetPickupOrdersQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<IReadOnlyList<AdminPickupOrderListItemDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AdminGetPickupOrderByIdQuery(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<AdminPickupOrderDto>.Success(result.Value, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AdminCreatePickupOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<long>.Success(result.Value, traceId, 200, "Created successfully"))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] AdminUpdatePickupOrderCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<bool>.Success(result.Value, traceId, 200, "Updated successfully"))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AdminDeletePickupOrderCommand(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<bool>.Success(result.Value, traceId, 200, "Deleted successfully"))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("{id}/stops/{stopId}/manifests")]
    public async Task<IActionResult> GetManifests(long id, long stopId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new EDCL.Module.Job.Application.Queries.AdminGetPickupOrderManifests.AdminGetPickupOrderManifestsQuery(id, stopId, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<IReadOnlyList<EDCL.Module.Job.Application.Queries.AdminGetPickupOrderById.AdminPickupOrderManifestDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    /// <summary>
    /// Assigns a driver to a specific pickup order job.
    /// </summary>
    /// <param name="id">Pickup Order ID</param>
    /// <param name="request">Request containing the driver ID to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Boolean indicating success.</returns>
    [HttpPost("{id}/assign")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AssignJob(long id, [FromBody] AssignJobRequest request, CancellationToken cancellationToken)
    {
        var command = new EDCL.Module.Job.Application.Commands.AssignJob.AssignJobCommand(id, request.DriverId, request.TruckId);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<bool>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    /// <summary>
    /// Forces completion of a job, ignoring geofence and driver checks.
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> ForceCompleteJob(long id, [FromBody] AdminCompleteJobRequest request, CancellationToken cancellationToken)
    {
        var command = new EDCL.Module.Job.Application.Commands.AdminCompleteJob.AdminCompleteJobCommand(id, request.Reason);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<bool>.Success(result.Value, traceId, 200, "Forced completion successful"))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }
}

public sealed record AdminCompleteJobRequest(string Reason);

public sealed record AssignJobRequest(long DriverId, long? TruckId = null);

