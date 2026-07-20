using Dapper;
using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Notification.Application.Queries.GetNotificationLogs;

public sealed class GetNotificationLogsQueryHandler(NotificationDbContext dbContext)
    : IRequestHandler<GetNotificationLogsQuery, Result<GetNotificationLogsResponse>>
{
    public async Task<Result<GetNotificationLogsResponse>> Handle(GetNotificationLogsQuery request, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        
        var conditions = new List<string> { "n.IsDeleted = 0" };
        var parameters = new DynamicParameters();

        if (request.DriverId.HasValue)
        {
            conditions.Add("n.DriverId = @DriverId");
            parameters.Add("DriverId", request.DriverId.Value);
        }

        if (!string.IsNullOrEmpty(request.DeliveryStatus))
        {
            conditions.Add("n.FcmDeliveryStatus = @DeliveryStatus");
            parameters.Add("DeliveryStatus", request.DeliveryStatus);
        }

        if (request.IsRead.HasValue)
        {
            conditions.Add("n.IsRead = @IsRead");
            parameters.Add("IsRead", request.IsRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            conditions.Add("(d.Name LIKE @Q OR p.delivery_no LIKE @Q OR n.Title LIKE @Q OR n.Message LIKE @Q)");
            parameters.Add("Q", $"%{request.Q}%");
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        
        var countSql = $@"
            SELECT COUNT(1) 
            FROM [notification].[driver_notifications] n
            LEFT JOIN [job].[pickup_orders] p ON p.Id = n.PickupOrderId
            LEFT JOIN [auth].[drivers] d ON d.Id = n.DriverId
            {whereClause}";
            
        var totalCount = await connection.ExecuteScalarAsync<long>(countSql, parameters);

        var offset = (request.Page - 1) * request.PageSize;
        
        var sql = $@"
            SELECT 
                n.Id, n.DriverId, d.Name as DriverName, n.Title, n.Message, n.Type, n.PickupOrderId, 
                p.delivery_no as PoNo, p.route_code as RouteCode, p.cycle_code as CycleCode, p.pickup_date as PickupDate,
                n.IsRead, n.FcmDeliveryStatus, n.FcmErrorMessage, n.AdminAlertAcknowledged, n.CreatedAt AT TIME ZONE 'UTC' as CreatedAt
            FROM [notification].[driver_notifications] n
            LEFT JOIN [job].[pickup_orders] p ON p.Id = n.PickupOrderId
            LEFT JOIN [auth].[drivers] d ON d.Id = n.DriverId
            {whereClause}
            ORDER BY n.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        var items = await connection.QueryAsync<NotificationLogDto>(sql, parameters);

        return Result<GetNotificationLogsResponse>.Success(
            new GetNotificationLogsResponse(items.ToList().AsReadOnly(), totalCount));
    }
}
