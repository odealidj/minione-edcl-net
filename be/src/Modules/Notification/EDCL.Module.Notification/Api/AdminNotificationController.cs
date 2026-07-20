using EDCL.Module.Notification.Application.Queries.GetNotificationLogs;
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
    /// <summary>
    /// Gets paginated notification logs (FCM status).
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? driverId = null,
        [FromQuery] string? deliveryStatus = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.GetTraceId();
        var query = new GetNotificationLogsQuery(page, pageSize, driverId, deliveryStatus);
        
        var result = await mediator.Send(query, cancellationToken);
        
        if (result.IsFailure)
        {
            var errors = new[] { new ApiError("notification", result.Error.Code, result.Error.Message) };
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400, errors));
        }

        var pagination = PaginationMeta.From(page, pageSize, result.Value.TotalCount);
        return Ok(ApiResponse<IReadOnlyList<NotificationLogDto>>.Paginated(result.Value.Items, pagination, traceId));
    }
}
