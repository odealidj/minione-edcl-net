using EDCL.Module.Job.Application.Commands.CompleteStop;
using EDCL.Module.Job.Application.Commands.EndJob;
using EDCL.Module.Job.Application.Commands.ScanKanban;
using EDCL.Module.Job.Application.Commands.StartJob;
using EDCL.Module.Job.Application.Queries.GetDashboard;
using EDCL.Module.Job.Application.Queries.GetRouteStops;
using EDCL.Shared.Http;
using EDCL.Shared.Http.Filters;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Job.Api;

/// <summary>
/// Controller for managing Driver Jobs, Route Stops, and Kanban Scans.
/// </summary>
[ApiController]
[Route("api/v1/jobs")]
[Authorize] // Enforce JWT for all job endpoints
public sealed class JobController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private long DriverId => currentUserService.DriverId ?? 0;

    /// <summary>
    /// Gets the dashboard data for the currently authenticated driver.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dashboard summary including active jobs and statistics.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<DashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Starts a specific pickup order job.
    /// </summary>
    /// <param name="id">Pickup Order ID</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Boolean indicating success.</returns>
    [HttpPost("{id}/start")]
    [TypeFilter(typeof(IdempotencyFilterAttribute))]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Retrieves all route stops for a specific pickup order job.
    /// </summary>
    /// <param name="id">Pickup Order ID</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of route stops and their status.</returns>
    [HttpGet("{id}/route-stops")]
    [ProducesResponseType(typeof(ApiResponse<RouteStopsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Marks a specific route stop as completed with Geofence validation.
    /// </summary>
    /// <param name="stopId">Route Stop ID</param>
    /// <param name="request">Request containing the driver's current coordinates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Boolean indicating success.</returns>
    [HttpPost("stops/{stopId}/complete")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Scans a kanban at a specific stop and manifest.
    /// </summary>
    /// <param name="stopId">Route Stop ID</param>
    /// <param name="manifestId">Cargo Manifest ID</param>
    /// <param name="request">Request containing the Kanban Code scanned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the kanban scan operation.</returns>
    [HttpPost("stops/{stopId}/manifests/{manifestId}/kanban")]
    [ProducesResponseType(typeof(ApiResponse<ScanKanbanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Ends a specific pickup order job.
    /// </summary>
    /// <param name="id">Pickup Order ID</param>
    /// <param name="request">Request containing the driver's current coordinates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Boolean indicating success.</returns>
    [HttpPost("{id}/end")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

/// <summary>Request payload for scanning a kanban.</summary>
public sealed record ScanKanbanRequest(string KanbanCode);
/// <summary>Request payload for ending a job.</summary>
public sealed record EndJobRequest(double Latitude, double Longitude);
/// <summary>Request payload for completing a route stop.</summary>
public sealed record CompleteStopRequest(double Latitude, double Longitude);
