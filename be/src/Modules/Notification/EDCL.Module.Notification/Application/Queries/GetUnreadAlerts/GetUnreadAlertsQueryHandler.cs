using Dapper;
using EDCL.Module.Notification.Application.Queries.GetNotificationLogs;
using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Notification.Application.Queries.GetUnreadAlerts;

public sealed class GetUnreadAlertsQueryHandler(NotificationDbContext dbContext)
    : IRequestHandler<GetUnreadAlertsQuery, Result<IReadOnlyList<NotificationLogDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationLogDto>>> Handle(GetUnreadAlertsQuery request, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        
        var sql = $@"
            SELECT 
                n.Id, n.DriverId, d.Name as DriverName, n.Title, n.Message, n.Type, n.PickupOrderId, 
                p.delivery_no as PoNo, p.route_code as RouteCode, p.cycle_code as CycleCode, p.pickup_date as PickupDate,
                n.IsRead, n.FcmDeliveryStatus, n.FcmErrorMessage, n.AdminAlertAcknowledged, n.CreatedAt
            FROM [notification].[driver_notifications] n
            LEFT JOIN [job].[pickup_orders] p ON p.Id = n.PickupOrderId
            LEFT JOIN [auth].[drivers] d ON d.Id = n.DriverId
            WHERE n.IsRead = 0 AND n.AdminAlertAcknowledged = 0 AND DATEDIFF(minute, n.CreatedAt, GETUTCDATE()) >= 15
            ORDER BY n.CreatedAt DESC";

        var items = await connection.QueryAsync<NotificationLogDto>(sql);

        return Result<IReadOnlyList<NotificationLogDto>>.Success(items.ToList().AsReadOnly());
    }
}
