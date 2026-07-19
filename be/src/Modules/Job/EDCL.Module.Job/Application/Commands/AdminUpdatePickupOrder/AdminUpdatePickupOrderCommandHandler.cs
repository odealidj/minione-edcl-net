using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Events;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MassTransit;
using EDCL.Module.Job.Application.Services;

namespace EDCL.Module.Job.Application.Commands.AdminUpdatePickupOrder;

internal sealed class AdminUpdatePickupOrderCommandHandler(JobDbContext dbContext, IMediator mediator, IPublishEndpoint publishEndpoint) 
    : IRequestHandler<AdminUpdatePickupOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AdminUpdatePickupOrderCommand request, CancellationToken cancellationToken)
    {
        var pickupOrder = await dbContext.PickupOrders
            .Include(x => x.Details).ThenInclude(x => x.Manifests).ThenInclude(x => x.Kanbans)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (pickupOrder == null)
            return Result<bool>.Failure(Error.NotFound("PickupOrder.NotFound", "Pickup order not found"));
            
        if (pickupOrder.Status != PickupOrderStatus.Pending)
            return Result<bool>.Failure(Error.Conflict("PickupOrder.InvalidStatus", "Cannot update pickup order that is not PENDING."));

        // Delete old scheduled jobs
        if (!string.IsNullOrEmpty(pickupOrder.HangfireJobIdH1))
        {
            BackgroundJob.Delete(pickupOrder.HangfireJobIdH1);
        }
        if (!string.IsNullOrEmpty(pickupOrder.HangfireJobIdH30))
        {
            BackgroundJob.Delete(pickupOrder.HangfireJobIdH30);
        }


        // Calculate manifest changes for events
        var oldManifestNos = pickupOrder.Details.SelectMany(d => d.Manifests).Select(m => m.ManifestNo).ToList();
        var newManifestNos = request.Stops.SelectMany(s => s.Manifests).Select(m => m.ManifestNo).ToList();

        var toUnassign = oldManifestNos.Except(newManifestNos).ToList();
        var toAssign = newManifestNos.Except(oldManifestNos).ToList();

        // Use reflection or just replace the whole graph if allowed, but EF Core requires careful graph updates.
        // For simplicity, we can do a full replacement of details if not Started.
        dbContext.PickupOrderDetails.RemoveRange(pickupOrder.Details);
        pickupOrder.Details.Clear();

        // Update basic info
        var entityType = typeof(PickupOrder);

        entityType.GetProperty("PickupDate")!.SetValue(pickupOrder, request.PickupDate);
        entityType.GetProperty("RouteCode")!.SetValue(pickupOrder, request.RouteCode);
        entityType.GetProperty("CycleCode")!.SetValue(pickupOrder, request.CycleCode);
        entityType.GetProperty("EstimatedDepartureTime")!.SetValue(pickupOrder, request.EstimatedDepartureTime);
        pickupOrder.Assign(request.DriverId, request.TruckId);

        foreach (var stopDto in request.Stops)
        {
            var stop = PickupOrderDetail.Create(pickupOrder.Id, stopDto.SupplierId, stopDto.Sequence);
            
            foreach (var manifestDto in stopDto.Manifests)
            {
                var manifest = PickupOrderManifest.Create(stop.Id, manifestDto.ManifestNo, manifestDto.TotalKanban, manifestDto.OrderType, manifestDto.TotalSkid, manifestDto.DockCode);
                stop.Manifests.Add(manifest);
            }
            
            pickupOrder.Details.Add(stop);
        }

        // Schedule Hangfire Reminders based on exact departure time (WIB = UTC+7)
        var departureTimeLocal = System.DateTime.SpecifyKind(pickupOrder.PickupDate.Date + pickupOrder.EstimatedDepartureTime, System.DateTimeKind.Unspecified);
        var departureTimeOffset = new System.DateTimeOffset(departureTimeLocal, System.TimeSpan.FromHours(7));

        var h1Time = departureTimeOffset.AddHours(-1);
        var h30Time = departureTimeOffset.AddMinutes(-30);
        
        string? jobIdH1 = null;
        string? jobIdH30 = null;

        if (h1Time > System.DateTimeOffset.UtcNow)
        {
            jobIdH1 = BackgroundJob.Schedule<IJobReminderService>(
                x => x.PublishReminderAsync(pickupOrder.Id, "H-1h"), 
                h1Time);
        }

        if (h30Time > System.DateTimeOffset.UtcNow)
        {
            jobIdH30 = BackgroundJob.Schedule<IJobReminderService>(
                x => x.PublishReminderAsync(pickupOrder.Id, "H-30m"), 
                h30Time);
        }

        pickupOrder.SetHangfireJobs(jobIdH1, jobIdH30);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (pickupOrder.DriverId.HasValue)
        {
            var assignedEvent = new JobAssignedIntegrationEvent
            {
                PickupOrderId = pickupOrder.Id,
                DriverId = pickupOrder.DriverId.Value,
                RouteCode = pickupOrder.RouteCode,
                Cycle = pickupOrder.CycleCode,
                PickupDate = pickupOrder.PickupDate
            };
            await publishEndpoint.Publish(assignedEvent, cancellationToken);
        }

        if (toUnassign.Any())
            await mediator.Publish(new ManifestsAssignedToRouteIntegrationEvent(toUnassign, false), cancellationToken);

        if (toAssign.Any())
            await mediator.Publish(new ManifestsAssignedToRouteIntegrationEvent(toAssign, true), cancellationToken);

        return Result<bool>.Success(true);
    }
}
