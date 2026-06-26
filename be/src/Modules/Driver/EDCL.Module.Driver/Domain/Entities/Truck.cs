using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Truck entity — represents a vehicle used for pickup.
/// Schema: [driver].[trucks]
/// </summary>
public sealed class Truck : AuditableEntity
{
    public long Id { get; private set; }
    public string PlateNumber { get; private set; } = default!;
    public string? VehicleType { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Truck() { } // EF Core

    public static Truck Create(string plateNumber, string? vehicleType = null)
        => new()
        {
            PlateNumber = plateNumber,
            VehicleType = vehicleType,
            IsActive = true
        };

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
