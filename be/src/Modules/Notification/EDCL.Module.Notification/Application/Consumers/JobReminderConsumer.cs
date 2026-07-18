using EDCL.Module.Notification.Domain.Entities;
using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Events;
using MassTransit;

using EDCL.Module.Notification.Infrastructure;
using EDCL.Shared.Kernel.Ports;

namespace EDCL.Module.Notification.Application.Consumers;

public class JobReminderConsumer(
    NotificationDbContext dbContext,
    IDriverPort driverPort,
    IFirebaseNotificationService firebaseNotificationService) : IConsumer<JobReminderIntegrationEvent>
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
            "REMINDER",
            message.PickupOrderId
        );

        dbContext.DriverNotifications.Add(notification);

        // Fetch DriverInfo to get FcmToken
        var driverInfo = await driverPort.GetActiveDriverByIdAsync(message.DriverId);
        
        if (driverInfo?.FcmToken != null)
        {
            var dataPayload = new Dictionary<string, string>
            {
                { "title", title },
                { "body", body },
                { "type", "REMINDER" },
                { "routeCode", message.RouteCode },
                { "cycle", message.Cycle.ToString() },
                { "pickupOrderId", message.PickupOrderId.ToString() }
            };

            var fcmResult = await firebaseNotificationService.SendDataNotificationAsync(driverInfo.FcmToken, dataPayload);
            
            if (fcmResult.IsSuccess)
                notification.MarkFcmAsSent();
            else
                notification.MarkFcmAsFailed(fcmResult.ErrorMessage ?? "Unknown error");
        }
        else
        {
            notification.MarkFcmAsFailed("FCM Token not found or Driver inactive");
        }

        await dbContext.SaveChangesAsync();
    }
}
