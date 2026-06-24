namespace EDCL.Module.Notification.Application.Queries.GetNotifications;

using Dapper;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

public sealed class GetNotificationsQueryHandler(IConfiguration configuration)
    : IRequestHandler<GetNotificationsQuery, Result<NotificationsResponse>>
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";

    public async Task<Result<NotificationsResponse>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT 
                Id AS NotificationId,
                Title,
                Message,
                IsRead,
                Type,
                PickupOrderId,
                CreatedAt
            FROM [notification].[driver_notifications]
            WHERE DriverId = @DriverId AND IsDeleted = 0
            ORDER BY CreatedAt DESC";

        var notifications = await connection.QueryAsync<NotificationDto>(sql, new { request.DriverId });
        var notifList = notifications.ToList();
        var unreadCount = notifList.Count(x => !x.IsRead);

        return new NotificationsResponse(request.DriverId, unreadCount, notifList);
    }
}
