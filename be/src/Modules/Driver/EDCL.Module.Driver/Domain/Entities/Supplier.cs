using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Supplier entity — represents a factory/vendor pickup location.
/// Schema: [driver].[suppliers]
/// </summary>
public sealed class Supplier : AuditableEntity
{
    public long Id { get; private set; }
    public string SupplierCode { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Address { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public int? GeofenceRadiusMeters { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Supplier() { } // EF Core

    public static Supplier Create(string code, string name, string? address = null,
        double? latitude = null, double? longitude = null, int? geofenceRadiusMeters = null)
        => new()
        {
            SupplierCode = code,
            Name = name,
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            GeofenceRadiusMeters = geofenceRadiusMeters,
            IsActive = true
        };

    public void UpdateLocation(double latitude, double longitude, int? radiusMeters = null)
    {
        Latitude = latitude;
        Longitude = longitude;
        GeofenceRadiusMeters = radiusMeters ?? GeofenceRadiusMeters;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
