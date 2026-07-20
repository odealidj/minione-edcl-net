using EDCL.Module.Notification.Application.Commands.AcknowledgeAlert;
using EDCL.Module.Notification.Application.Commands.ResendNotification;
using EDCL.Module.Notification.Application.Queries.GetNotificationLogs;
using EDCL.Module.Notification.Application.Queries.GetUnreadAlerts;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Notification.Api;

[ApiController]
[Route("api/v1/web/admin/notifications")]
[Authorize(Roles = "ADMIN")]
public sealed class AdminNotificationController(IMediator mediator) : ControllerBase
{
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? driverId = null,
        [FromQuery] string? deliveryStatus = null,
        [FromQuery] string? q = null,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.GetTraceId();
        var query = new GetNotificationLogsQuery(page, pageSize, driverId, deliveryStatus, q, isRead);
        
        var result = await mediator.Send(query, cancellationToken);
        
        if (result.IsFailure)
        {
            var errors = new[] { new ApiError("notification", result.Error.Code, result.Error.Message) };
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400, errors));
        }

        var pagination = PaginationMeta.From(page, pageSize, result.Value.TotalCount);
        return Ok(ApiResponse<IReadOnlyList<NotificationLogDto>>.Paginated(result.Value.Items, pagination, traceId));
    }

    [HttpGet("alerts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadAlerts(CancellationToken cancellationToken)
    {
        var traceId = HttpContext.GetTraceId();
        var query = new GetUnreadAlertsQuery();
        
        var result = await mediator.Send(query, cancellationToken);
        
        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));

        return Ok(ApiResponse<IReadOnlyList<NotificationLogDto>>.Success(result.Value, traceId));
    }

    [HttpPut("alerts/{id}/acknowledge")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcknowledgeAlert(long id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.GetTraceId();
        var command = new AcknowledgeAlertCommand(id);
        
        var result = await mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));

        return Ok(ApiResponse<bool>.Success(result.Value, traceId));
    }

    [HttpPost("{id}/resend")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendNotification(long id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.GetTraceId();
        var command = new ResendNotificationCommand(id);
        
        var result = await mediator.Send(command, cancellationToken);
        
        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));

        return Ok(ApiResponse<bool>.Success(result.Value, traceId));
    }
}
