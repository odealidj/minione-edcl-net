using EDCL.Module.Job.Application.Commands.CompleteStop;
using EDCL.Module.Job.Application.Commands.EndJob;
using EDCL.Module.Job.Application.Commands.ScanKanban;
using EDCL.Module.Job.Application.Commands.StartJob;
using EDCL.Module.Job.Application.Queries.GetDashboard;
using EDCL.Module.Job.Application.Queries.GetRouteStops;
using EDCL.Shared.Http;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Job.Api;

[ApiController]
[Route("api/v1/jobs")]
[Authorize] // Enforce JWT for all job endpoints
public sealed class JobController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private long DriverId => currentUserService.DriverId ?? 0;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var query = new GetDashboardQuery(DriverId);
        var result = await mediator.Send(query, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<DashboardResponse>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    [HttpPost("{id}/start")]
    public async Task<IActionResult> StartJob(long id, CancellationToken cancellationToken)
    {
        var command = new StartJobCommand(id, DriverId);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<bool>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    [HttpGet("{id}/route-stops")]
    public async Task<IActionResult> GetRouteStops(long id, CancellationToken cancellationToken)
    {
        var query = new GetRouteStopsQuery(id, DriverId);
        var result = await mediator.Send(query, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<RouteStopsResponse>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    [HttpPost("stops/{stopId}/complete")]
    public async Task<IActionResult> CompleteStop(long stopId, [FromBody] CompleteStopRequest request, CancellationToken cancellationToken)
    {
        var command = new CompleteStopCommand(stopId, DriverId, request.Latitude, request.Longitude);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<bool>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    [HttpPost("stops/{stopId}/manifests/{manifestId}/kanban")]
    public async Task<IActionResult> ScanKanban(long stopId, long manifestId, [FromBody] ScanKanbanRequest request, CancellationToken cancellationToken)
    {
        var command = new ScanKanbanCommand(stopId, manifestId, request.KanbanCode, DriverId);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<ScanKanbanResponse>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    [HttpPost("{id}/end")]
    public async Task<IActionResult> EndJob(long id, [FromBody] EndJobRequest request, CancellationToken cancellationToken)
    {
        var command = new EndJobCommand(id, DriverId, request.Latitude, request.Longitude);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;
        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<bool>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }
}

public sealed record ScanKanbanRequest(string KanbanCode);
public sealed record EndJobRequest(double Latitude, double Longitude);
public sealed record CompleteStopRequest(double Latitude, double Longitude);
