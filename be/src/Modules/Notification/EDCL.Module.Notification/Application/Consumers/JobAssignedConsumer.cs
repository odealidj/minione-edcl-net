using EDCL.Module.Notification.Domain.Entities;
using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Events;
using MassTransit;

namespace EDCL.Module.Notification.Application.Consumers;

public class JobAssignedConsumer(NotificationDbContext dbContext) : IConsumer<JobAssignedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<JobAssignedIntegrationEvent> context)
    {
        var message = context.Message;
        
        var body = $"Kamu telah ditugaskan untuk Route {message.RouteCode}, Cycle {message.Cycle} pada waktu {message.PickupDate:dd MMM yyyy HH:mm}. Mohon bersiap.";
        
        var notification = new DriverNotification(
            message.DriverId,
            "Tugas Baru Ditugaskan!",
            body,
            "ASSIGNMENT"
        );

        dbContext.DriverNotifications.Add(notification);
        await dbContext.SaveChangesAsync();
    }
}
