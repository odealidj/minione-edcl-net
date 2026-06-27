namespace EDCL.Shared.Kernel.Events;

public class JobAssignedIntegrationEvent
{
    public long PickupOrderId { get; init; }
    public long DriverId { get; init; }
    public string RouteCode { get; init; } = string.Empty;
    public string Cycle { get; init; } = string.Empty;
    public DateTime PickupDate { get; init; }
}
