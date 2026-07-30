namespace EDCL.Shared.Kernel.Events;

public class SimulationStartedIntegrationEvent
{
    public long TruckId { get; set; }
    public string GpsVehicleId { get; set; } = default!;
    public double StartLat { get; set; }
    public double StartLon { get; set; }
    public double EndLat { get; set; }
    public double EndLon { get; set; }
    public long JobId { get; set; } // Reference to PickupOrder
}
