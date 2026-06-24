namespace EDCL.Module.Notification.Api;

using EDCL.Module.Notification.Application.Commands.MarkNotificationRead;
using EDCL.Module.Notification.Application.Queries.GetNotifications;
using EDCL.Shared.Http;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        if (currentUserService.DriverId == null)
            return Unauthorized(ApiResponse<object>.Fail("DriverId is required", currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier, 401));

        var query = new GetNotificationsQuery(currentUserService.DriverId.Value);
        var result = await mediator.Send(query, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<NotificationsResponse>.Success(ok, traceId)),
            err => BadRequest(ApiResponse<object>.Fail(err.Message, traceId, 400))
        );
    }

    [HttpPost("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(long notificationId, CancellationToken cancellationToken)
    {
        if (currentUserService.DriverId == null)
            return Unauthorized(ApiResponse<object>.Fail("DriverId is required", currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier, 401));

        var command = new MarkNotificationReadCommand(notificationId, currentUserService.DriverId.Value);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = currentUserService.CurrentTraceId ?? HttpContext.TraceIdentifier;

        return result.Match<IActionResult>(
            ok => Ok(ApiResponse<bool>.Success(ok, traceId)),
            err => NotFound(ApiResponse<object>.Fail(err.Message, traceId, 404))
        );
    }
}
