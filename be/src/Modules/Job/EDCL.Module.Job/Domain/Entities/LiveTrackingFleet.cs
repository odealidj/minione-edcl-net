using EDCL.Shared.Kernel.Domain;
using System;

namespace EDCL.Module.Job.Domain.Entities;

/// <summary>
/// Represents a raw live tracking payload from GPS vendors.
/// Schema: [job].[live_tracking_fleets]
/// </summary>
public sealed class LiveTrackingFleet : AuditableEntity
{
    public long Id { get; private set; }
    
    // Optional FK to PickupOrder if we want to tie it directly, but usually it's just tied to the Truck.
    // For this use case, we'll include PickupOrderId if known at the time of recording.
    public long? PickupOrderId { get; private set; }
    
    public long TruckId { get; private set; }
    public long? DriverId { get; private set; }
    
    // Core GPS Data
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double Speed { get; private set; }
    public double? Heading { get; private set; }
    public double? Odometer { get; private set; }
    
    // Additional Info
    public string? Address { get; private set; }
    public bool? EngineStatus { get; private set; }
    
    // Metadata
    public DateTime RecordedAt { get; private set; }
    public string Provider { get; private set; } = default!;
    public string? RawData { get; private set; }

    private LiveTrackingFleet() { } // EF Core

    public static LiveTrackingFleet Create(
        long truckId,
        double latitude,
        double longitude,
        double speed,
        DateTime recordedAt,
        string provider,
        long? pickupOrderId = null,
        long? driverId = null,
        double? heading = null,
        double? odometer = null,
        string? address = null,
        bool? engineStatus = null,
        string? rawData = null)
    {
        return new LiveTrackingFleet
        {
            PickupOrderId = pickupOrderId,
            TruckId = truckId,
            DriverId = driverId,
            Latitude = latitude,
            Longitude = longitude,
            Speed = speed,
            Heading = heading,
            Odometer = odometer,
            Address = address,
            EngineStatus = engineStatus,
            RecordedAt = recordedAt,
            Provider = provider,
            RawData = rawData
        };
    }
}
