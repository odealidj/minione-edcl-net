using EDCL.Module.Job.Application.Commands.AdminRequeueBackgroundJob;
using EDCL.Module.Job.Application.Queries.AdminGetBackgroundJobs;
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
    public async Task<IActionResult> GetScheduledJobs([FromQuery] int from = 0, [FromQuery] int count = 100)
    {
        var result = await mediator.Send(new AdminGetScheduledJobsQuery(from, count));
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("failed")]
    public async Task<IActionResult> GetFailedJobs([FromQuery] int from = 0, [FromQuery] int count = 100)
    {
        var result = await mediator.Send(new AdminGetFailedJobsQuery(from, count));
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPost("requeue/{jobId}")]
    public async Task<IActionResult> RequeueJob(string jobId)
    {
        var result = await mediator.Send(new AdminRequeueBackgroundJobCommand(jobId));
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }
}
