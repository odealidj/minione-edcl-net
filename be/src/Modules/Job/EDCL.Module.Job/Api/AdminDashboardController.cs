using EDCL.Module.Job.Application.Queries.AdminGetDashboardSummary;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Job.Api;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/v1/web/admin/dashboard")]
public class AdminDashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<AdminGetDashboardSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary([FromQuery] AdminGetDashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<AdminGetDashboardSummaryResponse>.Success(result.Value, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("live-fleets")]
    [ProducesResponseType(typeof(ApiResponse<List<EDCL.Module.Job.Application.Queries.AdminGetLiveFleetTracking.TruckLocationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLiveFleets(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new EDCL.Module.Job.Application.Queries.AdminGetLiveFleetTracking.AdminGetLiveFleetTrackingQuery(), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess
            ? Ok(ApiResponse<List<EDCL.Module.Job.Application.Queries.AdminGetLiveFleetTracking.TruckLocationDto>>.Success(result.Value, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }
}
