using EDCL.Module.Notification.Domain.Entities;
using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Events;
using MassTransit;

using EDCL.Module.Notification.Infrastructure;
using EDCL.Shared.Kernel.Ports;

namespace EDCL.Module.Notification.Application.Consumers;

public class JobAssignedConsumer(
    NotificationDbContext dbContext,
    IDriverPort driverPort,
    IFirebaseNotificationService firebaseNotificationService) : IConsumer<JobAssignedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<JobAssignedIntegrationEvent> context)
    {
        var message = context.Message;
        
        var body = $"Kamu telah ditugaskan untuk Route {message.RouteCode}, Cycle {message.Cycle} pada waktu {message.PickupDate:dd MMM yyyy HH:mm}. Mohon bersiap.";
        
        var notification = new DriverNotification(
            message.DriverId,
            "Tugas Baru Ditugaskan!",
            body,
            "ASSIGNMENT",
            message.PickupOrderId
        );

        dbContext.DriverNotifications.Add(notification);

        // Fetch DriverInfo to get FcmToken
        var driverInfo = await driverPort.GetActiveDriverByIdAsync(message.DriverId);
        
        if (driverInfo?.FcmToken != null)
        {
            var dataPayload = new Dictionary<string, string>
            {
                { "type", "JOB_ASSIGNED" },
                { "pickupOrderId", context.Message.PickupOrderId.ToString() },
                { "routeCode", context.Message.RouteCode },
                { "pickupDate", context.Message.PickupDate.ToString("o") },
                { "title", "Tugas Baru Ditugaskan!" },
                { "body", body }
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
