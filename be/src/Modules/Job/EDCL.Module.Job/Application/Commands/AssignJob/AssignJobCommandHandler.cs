using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Events;
using EDCL.Module.Job.Application.Services;
using Hangfire;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Job.Application.Commands.AssignJob;

public sealed class AssignJobCommandHandler(
    JobDbContext dbContext,
    IPublishEndpoint publishEndpoint) : IRequestHandler<AssignJobCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AssignJobCommand request, CancellationToken cancellationToken)
    {
        var pickupOrder = await dbContext.PickupOrders
            .FirstOrDefaultAsync(x => x.Id == request.PickupOrderId, cancellationToken);

        if (pickupOrder == null)
            return Result<bool>.Failure(Error.NotFound("AssignJob.PickupOrderNotFound", "Pickup order not found."));

        // Delete old scheduled jobs if re-assigning
        if (!string.IsNullOrEmpty(pickupOrder.HangfireJobIdH1))
        {
            BackgroundJob.Delete(pickupOrder.HangfireJobIdH1);
        }
        if (!string.IsNullOrEmpty(pickupOrder.HangfireJobIdH30))
        {
            BackgroundJob.Delete(pickupOrder.HangfireJobIdH30);
        }

        pickupOrder.Assign(request.DriverId, request.TruckId);

        // Publish Immediate Notification Event
        var assignedEvent = new JobAssignedIntegrationEvent
        {
            PickupOrderId = pickupOrder.Id,
            DriverId = pickupOrder.DriverId!.Value,
            RouteCode = pickupOrder.RouteCode,
            Cycle = pickupOrder.CycleCode,
            PickupDate = pickupOrder.PickupDate
        };
        await publishEndpoint.Publish(assignedEvent, cancellationToken);

        // Schedule Hangfire Reminders based on exact departure time (WIB = UTC+7)
        var departureTimeLocal = DateTime.SpecifyKind(pickupOrder.PickupDate.Date + pickupOrder.EstimatedDepartureTime, DateTimeKind.Unspecified);
        var departureTimeOffset = new DateTimeOffset(departureTimeLocal, TimeSpan.FromHours(7));

        var h1Time = departureTimeOffset.AddHours(-1);
        var h30Time = departureTimeOffset.AddMinutes(-30);
        
        string? jobIdH1 = null;
        string? jobIdH30 = null;

        if (h1Time > DateTimeOffset.UtcNow)
        {
            jobIdH1 = BackgroundJob.Schedule<IJobReminderService>(
                x => x.PublishReminderAsync(pickupOrder.Id, "H-1h"), 
                h1Time);
        }

        if (h30Time > DateTimeOffset.UtcNow)
        {
            jobIdH30 = BackgroundJob.Schedule<IJobReminderService>(
                x => x.PublishReminderAsync(pickupOrder.Id, "H-30m"), 
                h30Time);
        }

        pickupOrder.SetHangfireJobs(jobIdH1, jobIdH30);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
