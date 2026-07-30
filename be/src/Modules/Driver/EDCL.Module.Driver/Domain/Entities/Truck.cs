using EDCL.Shared.Kernel.Domain;
using EDCL.Shared.Kernel.Common;
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
    public long LogisticPartnerId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // GPS & Simulator Configuration
    public string? GpsVehicleId { get; private set; }
    public bool IsSimulated { get; private set; } = false;

    // Navigation
    public LogisticPartner? LogisticPartner { get; private set; }
    public ICollection<TruckDriverAssignment> Assignments { get; private set; } = [];

    private Truck() { } // EF Core

    public static Truck Create(string plateNumber, long logisticPartnerId, string? vehicleType = null, bool isSimulated = false, string? gpsVehicleId = null)
        => new()
        {
            PlateNumber = plateNumber,
            LogisticPartnerId = logisticPartnerId,
            VehicleType = vehicleType,
            IsSimulated = isSimulated,
            GpsVehicleId = gpsVehicleId,
            IsActive = true
        };

    public void Update(string plateNumber, long logisticPartnerId, string? vehicleType, bool isSimulated = false, string? gpsVehicleId = null)
    {
        PlateNumber = plateNumber;
        LogisticPartnerId = logisticPartnerId;
        VehicleType = vehicleType;
        IsSimulated = isSimulated;
        GpsVehicleId = gpsVehicleId;
    }

    public void ConfigureGps(string? gpsVehicleId, bool isSimulated)
    {
        GpsVehicleId = gpsVehicleId;
        IsSimulated = isSimulated;
    }

    public Result AssignDriver(long driverId)
    {
        // Deactivate current active assignments for this truck
        foreach (var assignment in Assignments.Where(a => a.IsActive))
        {
            assignment.Unassign();
        }

        // Add new active assignment
        Assignments.Add(TruckDriverAssignment.Create(Id, driverId));
        return Result.Success();
    }

    public Result UnassignDriver(long driverId)
    {
        var assignment = Assignments.FirstOrDefault(a => a.DriverId == driverId && a.IsActive);
        if (assignment is null) return Result.Failure(new Error("Truck.DriverNotAssigned", "The specified driver is not actively assigned to this truck."));
        
        assignment.Unassign();
        return Result.Success();
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
