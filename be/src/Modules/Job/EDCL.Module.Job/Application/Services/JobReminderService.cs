using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Job.Application.Services;

public interface IJobReminderService
{
    Task PublishReminderAsync(long pickupOrderId, string reminderType);
}

public class JobReminderService(JobDbContext dbContext, IPublishEndpoint publishEndpoint) : IJobReminderService
{
    public async Task PublishReminderAsync(long pickupOrderId, string reminderType)
    {
        var pickupOrder = await dbContext.PickupOrders
            .FirstOrDefaultAsync(x => x.Id == pickupOrderId);

        if (pickupOrder == null || pickupOrder.Status == "CANCELLED")
            return; // Job is no longer valid

        var reminderEvent = new JobReminderIntegrationEvent
        {
            PickupOrderId = pickupOrder.Id,
            DriverId = pickupOrder.DriverId,
            RouteCode = pickupOrder.RouteCode,
            Cycle = pickupOrder.CycleCode,
            PickupDate = pickupOrder.PickupDate,
            ReminderType = reminderType
        };

        await publishEndpoint.Publish(reminderEvent);
    }
}
