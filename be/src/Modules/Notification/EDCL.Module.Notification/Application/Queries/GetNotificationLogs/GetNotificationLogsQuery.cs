using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Notification.Application.Queries.GetNotificationLogs;

public sealed record NotificationLogDto(
    long Id,
    long DriverId,
    string Title,
    string Message,
    string Type,
    long? PickupOrderId,
    bool IsRead,
    string? FcmDeliveryStatus,
    string? FcmErrorMessage,
    DateTime CreatedAtUtc);

public sealed record GetNotificationLogsResponse(
    IReadOnlyList<NotificationLogDto> Items,
    long TotalCount);

public sealed record GetNotificationLogsQuery(
    int Page,
    int PageSize,
    long? DriverId = null,
    string? DeliveryStatus = null) : IRequest<Result<GetNotificationLogsResponse>>;
