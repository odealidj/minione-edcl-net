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
        
        var conditions = new List<string> { "IsDeleted = 0" };
        var parameters = new DynamicParameters();

        if (request.DriverId.HasValue)
        {
            conditions.Add("DriverId = @DriverId");
            parameters.Add("DriverId", request.DriverId.Value);
        }

        if (!string.IsNullOrEmpty(request.DeliveryStatus))
        {
            conditions.Add("FcmDeliveryStatus = @DeliveryStatus");
            parameters.Add("DeliveryStatus", request.DeliveryStatus);
        }

        var whereClause = string.Join(" AND ", conditions);
        
        var countSql = $"SELECT COUNT(1) FROM [notification].[driver_notifications] WHERE {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<long>(countSql, parameters);

        var offset = (request.Page - 1) * request.PageSize;
        
        var sql = $@"
            SELECT 
                Id, DriverId, Title, Message, Type, PickupOrderId, IsRead, 
                FcmDeliveryStatus, FcmErrorMessage, CreatedAtUtc
            FROM [notification].[driver_notifications]
            WHERE {whereClause}
            ORDER BY CreatedAtUtc DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        var items = await connection.QueryAsync<NotificationLogDto>(sql, parameters);

        return Result<GetNotificationLogsResponse>.Success(
            new GetNotificationLogsResponse(items.ToList().AsReadOnly(), totalCount));
    }
}
