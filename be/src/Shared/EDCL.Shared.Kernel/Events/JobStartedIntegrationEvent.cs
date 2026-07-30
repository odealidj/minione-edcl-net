namespace EDCL.Shared.Kernel.Events;

public class JobStartedIntegrationEvent
{
    public long JobId { get; set; }
    public long TruckId { get; set; }
}
