using EDCL.Module.Job.Application.Commands.AdminRequeueBackgroundJob;
using EDCL.Module.Job.Application.Queries.AdminGetBackgroundJobs;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Job.Api;

[Authorize(Roles = "ADMIN")]
[ApiController]
[Route("api/v1/web/admin/system-tasks")]
public class AdminBackgroundJobsController(IMediator mediator) : ControllerBase
{
    [HttpGet("scheduled")]
    [ProducesResponseType(typeof(ApiResponse<List<BackgroundJobDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduledJobs([FromQuery] int from = 0, [FromQuery] int count = 100)
    {
        var result = await mediator.Send(new AdminGetScheduledJobsQuery(from, count));
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<List<BackgroundJobDto>>.Success(result.Value, traceId)) 
            : BadRequest(ApiResponse<object>.Fail(result.Error?.Message ?? "Error", traceId, 400));
    }

    [HttpGet("failed")]
    [ProducesResponseType(typeof(ApiResponse<List<BackgroundJobDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedJobs([FromQuery] int from = 0, [FromQuery] int count = 100)
    {
        var result = await mediator.Send(new AdminGetFailedJobsQuery(from, count));
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<List<BackgroundJobDto>>.Success(result.Value, traceId)) 
            : BadRequest(ApiResponse<object>.Fail(result.Error?.Message ?? "Error", traceId, 400));
    }

    [HttpPost("requeue/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequeueJob(string jobId)
    {
        var result = await mediator.Send(new AdminRequeueBackgroundJobCommand(jobId));
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<bool>.Success(result.Value, traceId)) 
            : BadRequest(ApiResponse<object>.Fail(result.Error?.Message ?? "Error", traceId, 400));
    }
}
