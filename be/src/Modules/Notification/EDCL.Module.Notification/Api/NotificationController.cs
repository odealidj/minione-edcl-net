namespace EDCL.Module.Notification.Api;

using EDCL.Module.Notification.Application.Commands.MarkNotificationRead;
using EDCL.Module.Notification.Application.Queries.GetNotifications;
using EDCL.Shared.Http;
using EDCL.Shared.Http.Responses;
using EDCL.Shared.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for managing driver notifications.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    /// <summary>
    /// Retrieves all notifications for the currently authenticated driver.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of notifications.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<NotificationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="notificationId">The ID of the notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    [HttpPost("{notificationId}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
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
