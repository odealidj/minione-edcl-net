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
[Route("api/v1/admin/pickup-orders")]
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
}
