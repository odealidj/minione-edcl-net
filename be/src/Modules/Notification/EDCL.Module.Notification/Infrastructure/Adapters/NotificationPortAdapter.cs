using EDCL.Module.Notification.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Notification.Infrastructure.Adapters;

/// <summary>
/// Real implementation of INotificationPort — reads from [notification].[driver_notifications] table.
/// Registered in NotificationModuleRegistration and injected into other modules via DI.
/// </summary>
public sealed class NotificationPortAdapter(NotificationDbContext db) : INotificationPort
{
    public async Task<IReadOnlyDictionary<long, NotificationStatusInfo>> GetNotificationStatusesByReferenceIdsAsync(
        IEnumerable<long> referenceIds, 
        string type, 
        CancellationToken ct = default)
    {
        var refIds = referenceIds.ToList();
        
        if (!refIds.Any())
            return new Dictionary<long, NotificationStatusInfo>();

        var notifications = await db.DriverNotifications
            .AsNoTracking()
            .Where(n => n.Type == type && n.PickupOrderId != null && refIds.Contains(n.PickupOrderId.Value))
            .ToListAsync(ct);

        // Group by Reference ID and pick the latest one if multiple
        var result = new Dictionary<long, NotificationStatusInfo>();

        foreach (var group in notifications.GroupBy(n => n.PickupOrderId!.Value))
        {
            var latest = group.OrderByDescending(n => n.CreatedAt).First();
            result[group.Key] = new NotificationStatusInfo(
                Id: latest.Id,
                FcmDeliveryStatus: latest.FcmDeliveryStatus,
                FcmErrorMessage: latest.FcmErrorMessage,
                IsRead: latest.IsRead
            );
        }

        return result;
    }
}
