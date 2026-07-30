using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Represents the latest known location of a truck.
/// Schema: [driver].[truck_locations]
/// </summary>
public sealed class TruckLocation : AuditableEntity
{
    public long Id { get; private set; }
    public long TruckId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double? Speed { get; private set; }
    public double? Heading { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? ProviderName { get; private set; }
    
    // Navigation
    public Truck? Truck { get; private set; }

    private TruckLocation() { }

    public static TruckLocation Create(
        long truckId, 
        double latitude, 
        double longitude, 
        double? speed, 
        double? heading, 
        DateTime timestamp, 
        string? providerName)
    {
        return new TruckLocation
        {
            TruckId = truckId,
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            Heading = heading,
            Timestamp = timestamp,
            ProviderName = providerName
        };
    }

    public void UpdateLocation(
        double latitude, 
        double longitude, 
        double? speed, 
        double? heading, 
        DateTime timestamp, 
        string? providerName)
    {
        Latitude = latitude;
        Longitude = longitude;
        Speed = speed;
        Heading = heading;
        Timestamp = timestamp;
        ProviderName = providerName;
    }
}
