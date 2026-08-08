using EDCL.Shared.Kernel.Common;
using EDCL.Module.Job.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace EDCL.Module.Job.Application.Queries.AdminGetLiveFleetTracking;

public record AdminGetLiveFleetTrackingQuery : IRequest<Result<List<TruckLocationDto>>>;

public class TruckLocationDto
{
    public long TruckId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public double Heading { get; set; }
    public DateTime Timestamp { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? DeliveryNo { get; set; }
}

public class AdminGetLiveFleetTrackingQueryHandler(JobDbContext context) : IRequestHandler<AdminGetLiveFleetTrackingQuery, Result<List<TruckLocationDto>>>
{
    public async Task<Result<List<TruckLocationDto>>> Handle(AdminGetLiveFleetTrackingQuery request, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var query = @"
            SELECT 
                TruckId,
                Latitude,
                Longitude,
                Speed,
                Heading,
                Timestamp,
                ProviderName,
                IsConnected,
                PlateNumber,
                DeliveryNo
            FROM (
                SELECT 
                    t.Id as TruckId,
                    tl.Latitude,
                    tl.Longitude,
                    tl.Speed,
                    tl.Heading,
                    tl.Timestamp,
                    tl.ProviderName,
                    t.PlateNumber,
                    (SELECT TOP 1 po.delivery_no FROM job.pickup_orders po WHERE po.TruckId = t.Id AND po.Status = 'ON_PROGRESS' ORDER BY po.CreatedAt DESC) as DeliveryNo,
                    CASE 
                        WHEN mapping.LastGpsSyncStatus = 'Success' 
                             AND DATEDIFF(minute, mapping.LastGpsSyncAt, GETUTCDATE()) <= 30 
                        THEN CAST(1 AS BIT) 
                        ELSE CAST(0 AS BIT) 
                    END as IsConnected,
                    ROW_NUMBER() OVER (PARTITION BY t.Id ORDER BY tl.Timestamp DESC) as rn
                FROM driver.trucks t
                INNER JOIN driver.truck_locations tl ON t.Id = tl.TruckId
                LEFT JOIN driver.logistic_partner_gps_vendors mapping ON t.LogisticPartnerId = mapping.LogisticPartnerId
                WHERE t.is_deleted = 0
            ) result
            WHERE rn = 1 AND DeliveryNo IS NOT NULL
        ";

        var latestLocations = await connection.QueryAsync<TruckLocationDto>(query);
        return Result<List<TruckLocationDto>>.Success(latestLocations.AsList());
    }
}
