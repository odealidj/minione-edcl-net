namespace EDCL.Worker.GpsTracker.Models;

public class NormalizedGpsPoint
{
    public string GpsVehicleId { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsEngineOn { get; set; }
    public string ProviderName { get; set; } = default!;
}
