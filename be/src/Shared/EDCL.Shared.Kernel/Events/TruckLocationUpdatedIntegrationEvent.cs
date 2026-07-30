namespace EDCL.Shared.Kernel.Events;

public class TruckLocationUpdatedIntegrationEvent
{
    public long TruckId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
    public string ProviderName { get; set; } = default!;
}
