namespace EDCL.Module.Notification.Domain.Entities;

using EDCL.Shared.Kernel.Domain;

public class DriverNotification : AuditableEntity
{
    public long Id { get; private set; }
    public long DriverId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public bool IsRead { get; private set; }
    public string Type { get; private set; } = default!;
    public long? PickupOrderId { get; private set; } // Reference to a job if applicable
    public string? FcmDeliveryStatus { get; private set; } // "Pending", "Sent", "Failed"
    public string? FcmErrorMessage { get; private set; }

    protected DriverNotification() { } // EF Core

    public DriverNotification(long driverId, string title, string message, string type, long? pickupOrderId = null)
    {
        DriverId = driverId;
        Title = title;
        Message = message;
        Type = type;
        PickupOrderId = pickupOrderId;
        IsRead = false;
        FcmDeliveryStatus = "Pending";
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }

    public void MarkFcmAsSent()
    {
        FcmDeliveryStatus = "Sent";
    }

    public void MarkFcmAsFailed(string error)
    {
        FcmDeliveryStatus = "Failed";
        FcmErrorMessage = error;
    }
}
