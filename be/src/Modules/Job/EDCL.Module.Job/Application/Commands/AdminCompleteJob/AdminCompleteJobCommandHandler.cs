using Dapper;
using EDCL.Module.Job.Application.Ports;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Events;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Commands.AdminCompleteJob;

public sealed class AdminCompleteJobCommandHandler(
    IPickupOrderRepository repository,
    JobDbContext context,
    IPublishEndpoint publishEndpoint,
    IJobNotificationPort notificationPort) : IRequestHandler<AdminCompleteJobCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AdminCompleteJobCommand request, CancellationToken cancellationToken)
    {
        var job = await context.PickupOrders
            .Include(x => x.Details)
                .ThenInclude(d => d.Manifests)
            .FirstOrDefaultAsync(x => x.Id == request.PickupOrderId, cancellationToken);

        if (job == null)
            return Error.NotFound("Job.NotFound", "Pickup order not found.");

        double? lastLat = null;
        double? lastLon = null;

        if (job.TruckId.HasValue)
        {
            var connection = context.Database.GetDbConnection();
            var sql = @"
                SELECT TOP 1 Latitude, Longitude 
                FROM driver.truck_locations 
                WHERE TruckId = @TruckId 
                ORDER BY Timestamp DESC";

            var location = await connection.QueryFirstOrDefaultAsync<(double Latitude, double Longitude)>(
                sql, 
                new { TruckId = job.TruckId.Value });

            if (location != default)
            {
                lastLat = location.Latitude;
                lastLon = location.Longitude;
            }
        }

        try
        {
            job.ForceComplete(lastLat, lastLon, request.Reason);
            await repository.UpdateAsync(job, cancellationToken);

            // Publish ManifestDeliveredIntegrationEvent for all manifests
            var manifests = job.Details.SelectMany(d => d.Manifests).ToList();
            foreach (var manifest in manifests)
            {
                var evt = new ManifestDeliveredIntegrationEvent
                {
                    ManifestId = manifest.Id,
                    ManifestNo = manifest.ManifestNo,
                    Status = "Delivered", // Trigger IDCS sync
                    DeliveredAt = DateTime.UtcNow,
                    Remarks = $"Force completed: {request.Reason}"
                };
                await publishEndpoint.Publish(evt, cancellationToken);
            }

            if (job.DriverId.HasValue)
            {
                // Notify driver/system so mobile app knows it's finished
                await notificationPort.NotifyDriverJobCompletedAsync(job.DriverId.Value, job.Id, cancellationToken);
            }

            return true;
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("Job.InvalidState", ex.Message);
        }
    }
}
