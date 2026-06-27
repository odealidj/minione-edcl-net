using EDCL.Module.Notification.Domain.Entities;
using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Events;
using MassTransit;

namespace EDCL.Module.Notification.Application.Consumers;

public class JobReminderConsumer(NotificationDbContext dbContext) : IConsumer<JobReminderIntegrationEvent>
{
    public async Task Consume(ConsumeContext<JobReminderIntegrationEvent> context)
    {
        var message = context.Message;
        
        string title = message.ReminderType == "H-1h" ? "Reminder: 1 Jam Menuju Penjemputan" : "Reminder: 30 Menit Menuju Penjemputan";
        string body = $"Route {message.RouteCode}, Cycle {message.Cycle} dijadwalkan pada {message.PickupDate:HH:mm}.";

        var notification = new DriverNotification(
            message.DriverId,
            title,
            body,
            "REMINDER"
        );

        dbContext.DriverNotifications.Add(notification);
        await dbContext.SaveChangesAsync();
    }
}
