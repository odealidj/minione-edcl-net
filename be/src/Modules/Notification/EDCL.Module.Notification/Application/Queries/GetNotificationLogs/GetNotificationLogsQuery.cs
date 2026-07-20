using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Notification.Application.Queries.GetNotificationLogs;

public sealed record NotificationLogDto
{
    public long Id { get; init; }
    public long DriverId { get; init; }
    public string DriverName { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Message { get; init; } = default!;
    public string Type { get; init; } = default!;
    public long? PickupOrderId { get; init; }
    public string? PoNo { get; init; }
    public string? RouteCode { get; init; }
    public string? CycleCode { get; init; }
    public DateTime? PickupDate { get; init; }
    public bool IsRead { get; init; }
    public string? FcmDeliveryStatus { get; init; }
    public string? FcmErrorMessage { get; init; }
    public bool AdminAlertAcknowledged { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record GetNotificationLogsResponse(
    IReadOnlyList<NotificationLogDto> Items,
    long TotalCount);

public sealed record GetNotificationLogsQuery(
    int Page = 1,
    int PageSize = 10,
    long? DriverId = null,
    string? DeliveryStatus = null,
    string? Q = null,
    bool? IsRead = null) : IRequest<Result<GetNotificationLogsResponse>>;
