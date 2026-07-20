using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using EDCL.Shared.Kernel.Events;
using Hangfire;

using MassTransit;
using EDCL.Module.Job.Application.Services;

namespace EDCL.Module.Job.Application.Commands.AdminCreatePickupOrder;

internal sealed class AdminCreatePickupOrderCommandHandler(JobDbContext dbContext, IMediator mediator, IPublishEndpoint publishEndpoint)
    : IRequestHandler<AdminCreatePickupOrderCommand, Result<long>>
{
    public async Task<Result<long>> Handle(AdminCreatePickupOrderCommand request, CancellationToken cancellationToken)
    {
        var todayPrefix = System.DateTime.UtcNow.AddHours(7).ToString("yyyyMMdd");
        var countToday = await dbContext.PickupOrders.CountAsync(x => x.PoNo.StartsWith(todayPrefix), cancellationToken);
        var generatedPoNo = $"{todayPrefix}{(countToday + 1):D4}";

        var requestedManifestNos = request.Stops.SelectMany(s => s.Manifests).Select(m => m.ManifestNo).ToList();
        
        var conflictingManifests = await dbContext.PickupOrders
            .Where(po => po.Status == PickupOrderStatus.Pending || po.Status == PickupOrderStatus.OnProgress)
            .SelectMany(po => po.Details)
            .SelectMany(d => d.Manifests)
            .Where(m => requestedManifestNos.Contains(m.ManifestNo))
            .Select(m => m.ManifestNo)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (conflictingManifests.Any())
        {
            var conflicts = string.Join(", ", conflictingManifests);
            return Result<long>.Failure(Error.Conflict("Manifest.DoubleBooking", $"The following manifests are already assigned to an active route: {conflicts}"));
        }

        var pickupOrder = PickupOrder.Create(request.DriverId, request.TruckId, generatedPoNo, request.PickupDate, request.RouteCode, request.CycleCode, request.EstimatedDepartureTime);

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

        dbContext.PickupOrders.Add(pickupOrder);
        await dbContext.SaveChangesAsync(cancellationToken);

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
                PickupDate = pickupOrder.PickupDate.Date + pickupOrder.EstimatedDepartureTime
            };
            await publishEndpoint.Publish(assignedEvent, cancellationToken);
        }

        // Publish event asynchronously via Hangfire for resilience (retries if Cargo DB fails)
        await mediator.Publish(new ManifestsAssignedToRouteIntegrationEvent(requestedManifestNos, true), cancellationToken);

        return Result<long>.Success(pickupOrder.Id);
    }
}
