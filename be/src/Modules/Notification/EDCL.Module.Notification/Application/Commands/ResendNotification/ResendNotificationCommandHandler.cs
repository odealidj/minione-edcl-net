using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Module.Notification.Infrastructure;
using EDCL.Shared.Kernel.Ports;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Notification.Application.Commands.ResendNotification;

public sealed class ResendNotificationCommandHandler(
    NotificationDbContext dbContext,
    IDriverPort driverPort,
    IFirebaseNotificationService firebaseNotificationService)
    : IRequestHandler<ResendNotificationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ResendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await dbContext.DriverNotifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken);

        if (notification == null)
            return Error.NotFound("Notification", request.NotificationId);

        var driverInfo = await driverPort.GetActiveDriverByIdAsync(notification.DriverId);
        
        if (driverInfo?.FcmToken != null)
        {
            var dataPayload = new Dictionary<string, string>
            {
                { "type", notification.Type },
                { "title", notification.Title },
                { "body", notification.Message },
                { "notificationId", notification.Id.ToString() }
            };
            
            if (notification.PickupOrderId.HasValue)
                dataPayload.Add("pickupOrderId", notification.PickupOrderId.Value.ToString());

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

        // Reset Admin Alert Acknowledged so it can alert again if they still don't read it?
        // Wait, if we resend, the CreatedAt is NOT changed!
        // We shouldn't reset CreatedAt because it's an AuditableEntity, but they resend it.
        // Let's just leave it, or maybe we don't need to touch AcknowledgeAdminAlert here.

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
