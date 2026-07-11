using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Represents the assignment of a Driver to a Truck.
/// Schema: [driver].[truck_driver_assignments]
/// </summary>
public sealed class TruckDriverAssignment : AuditableEntity
{
    public long Id { get; private set; }
    public long TruckId { get; private set; }
    public long DriverId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public DateTime? UnassignedAt { get; private set; }

    public Truck? Truck { get; private set; }

    private TruckDriverAssignment() { }

    public static TruckDriverAssignment Create(long truckId, long driverId)
        => new()
        {
            TruckId = truckId,
            DriverId = driverId,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };

    public void Unassign()
    {
        if (!IsActive) return;
        IsActive = false;
        UnassignedAt = DateTime.UtcNow;
    }
}
