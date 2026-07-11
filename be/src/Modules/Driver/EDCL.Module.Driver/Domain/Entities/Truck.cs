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
    public long TransporterId { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Transporter? Transporter { get; private set; }
    public ICollection<TruckDriverAssignment> Assignments { get; private set; } = [];

    private Truck() { } // EF Core

    public static Truck Create(string plateNumber, long transporterId, string? vehicleType = null)
        => new()
        {
            PlateNumber = plateNumber,
            TransporterId = transporterId,
            VehicleType = vehicleType,
            IsActive = true
        };

    public void Update(string plateNumber, long transporterId, string? vehicleType)
    {
        PlateNumber = plateNumber;
        TransporterId = transporterId;
        VehicleType = vehicleType;
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
