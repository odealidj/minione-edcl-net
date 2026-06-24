namespace EDCL.Module.Notification.Application.Queries.GetNotifications;

using EDCL.Shared.Kernel.Common;
using MediatR;

public sealed record GetNotificationsQuery(long DriverId) : IRequest<Result<NotificationsResponse>>;

public sealed record NotificationsResponse(
    long DriverId,
    int UnreadCount,
    IEnumerable<NotificationDto> Notifications);

public sealed record NotificationDto(
    long NotificationId,
    string Title,
    string Message,
    bool IsRead,
    string Type,
    long? PickupOrderId,
    DateTime CreatedAt);
