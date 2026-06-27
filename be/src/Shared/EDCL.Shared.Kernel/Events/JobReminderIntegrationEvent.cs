namespace EDCL.Shared.Kernel.Events;

public class JobReminderIntegrationEvent
{
    public long PickupOrderId { get; init; }
    public long DriverId { get; init; }
    public string RouteCode { get; init; } = string.Empty;
    public string Cycle { get; init; } = string.Empty;
    public DateTime PickupDate { get; init; }
    public string ReminderType { get; init; } = string.Empty; // e.g. "H-1h" or "H-30m"
}
