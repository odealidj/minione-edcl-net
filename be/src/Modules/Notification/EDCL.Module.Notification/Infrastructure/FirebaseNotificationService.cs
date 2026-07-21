using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Notification.Infrastructure;

public interface IFirebaseNotificationService
{
    Task<(bool IsSuccess, string? ErrorMessage)> SendDataNotificationAsync(string fcmToken, Dictionary<string, string> data);
}

public class FirebaseNotificationService(ILogger<FirebaseNotificationService> logger) : IFirebaseNotificationService
{
    public async Task<(bool IsSuccess, string? ErrorMessage)> SendDataNotificationAsync(string fcmToken, Dictionary<string, string> data)
    {
        if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
        {
            logger.LogWarning("FirebaseApp is not initialized. Cannot send push notification.");
            return (false, "FirebaseApp is not initialized.");
        }

        try
        {
            data.TryGetValue("title", out var title);
            data.TryGetValue("body", out var body);

            var message = new Message()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Token = fcmToken,
#pragma warning restore CS0618 // Type or member is obsolete
                Notification = (title != null || body != null) ? new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                } : null,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = "edcl_fcm_channel"
                    }
                },
                Data = data
            };

            // Send a message to the device corresponding to the provided registration token.
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            logger.LogInformation("Successfully sent message: {response}", response);
            
            return (true, null);
        }
        catch (FirebaseMessagingException ex)
        {
            logger.LogError(ex, "Error sending FCM message. Code: {Code}", ex.MessagingErrorCode);
            return (false, $"Firebase Error: {ex.MessagingErrorCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error sending FCM message.");
            return (false, ex.Message);
        }
    }
}
