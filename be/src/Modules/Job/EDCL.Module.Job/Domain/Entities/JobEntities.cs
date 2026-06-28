using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Job.Domain.Entities;

/// <summary>
/// Represents a driver's pickup job for one shift.
/// Schema: [job].[pickup_orders]
/// </summary>
public sealed class PickupOrder : AuditableEntity
{
    public long Id { get; private set; }
    public long DriverId { get; private set; }
    public long? TruckId { get; private set; }
    public string PoNo { get; private set; } = default!;
    public DateTime PickupDate { get; private set; }
    public string RouteCode { get; private set; } = default!;
    public string CycleCode { get; private set; } = default!;
    public TimeSpan EstimatedDepartureTime { get; private set; }
    public string Status { get; private set; } = PickupOrderStatus.Pending;
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public string? HangfireJobIdH1 { get; private set; }
    public string? HangfireJobIdH30 { get; private set; }

    public ICollection<PickupOrderDetail> Details { get; private set; } = [];
    public ICollection<PickupOrderManifest> Manifests { get; private set; } = [];

    private PickupOrder() { }

    public static PickupOrder Create(long driverId, long? truckId, string poNo, DateTime pickupDate, string routeCode, string cycleCode, TimeSpan estimatedDepartureTime)
        => new() { DriverId = driverId, TruckId = truckId, PoNo = poNo, PickupDate = pickupDate, RouteCode = routeCode, CycleCode = cycleCode, EstimatedDepartureTime = estimatedDepartureTime, Status = PickupOrderStatus.Pending };

    public void Assign(long driverId, long? truckId)
    {
        DriverId = driverId;
        TruckId = truckId;
    }

    public void SetHangfireJobs(string? h1, string? h30)
    {
        HangfireJobIdH1 = h1;
        HangfireJobIdH30 = h30;
    }

    public void Start()
    {
        if (Status != PickupOrderStatus.Pending)
            throw new InvalidOperationException($"Cannot start job in status '{Status}'.");
        Status = PickupOrderStatus.OnProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != PickupOrderStatus.OnProgress)
            throw new InvalidOperationException($"Cannot complete job in status '{Status}'.");
            
        if (Details.Any(d => d.Status != StopStatus.PickedUp))
            throw new InvalidOperationException("Cannot complete job because there are uncompleted stops (suppliers).");
            
        Status = PickupOrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}

public static class PickupOrderStatus
{
    public const string Pending    = "PENDING";
    public const string OnProgress = "ON_PROGRESS";
    public const string Completed  = "COMPLETED";
    public const string Cancelled  = "CANCELLED";
}

/// <summary>
/// One stop (supplier) within a pickup job.
/// Schema: [job].[pickup_order_details]
/// </summary>
public sealed class PickupOrderDetail : AuditableEntity
{
    public long Id { get; private set; }
    public long PickupOrderId { get; private set; }
    public long SupplierId { get; private set; }
    public int Sequence { get; private set; }
    public string Status { get; private set; } = StopStatus.Pending;
    public DateTime? ArrivedAt { get; private set; }
    public DateTime? PickedUpAt { get; private set; }

    public PickupOrder? PickupOrder { get; private set; }
    public ICollection<PickupOrderManifest> Manifests { get; private set; } = [];

    private PickupOrderDetail() { }

    public static PickupOrderDetail Create(long pickupOrderId, long supplierId, int sequence)
        => new() { PickupOrderId = pickupOrderId, SupplierId = supplierId, Sequence = sequence };

    public void MarkArrived()  => ArrivedAt  = DateTime.UtcNow;
    public void MarkPickedUp()
    {
        Status     = StopStatus.PickedUp;
        PickedUpAt = DateTime.UtcNow;
    }
}

public static class StopStatus
{
    public const string Pending  = "PENDING";
    public const string PickedUp = "PICKED_UP";
    public const string Skipped  = "SKIPPED";
}

/// <summary>
/// Links a manifest to a specific stop in a pickup job.
/// Also tracks scanning progress (original/total counts).
/// Schema: [job].[pickup_order_manifests]
/// </summary>
public sealed class PickupOrderManifest : AuditableEntity
{
    public long Id { get; private set; }
    public long PickupOrderDetailId { get; private set; }
    public string ManifestNo { get; private set; } = default!;
    public string Status { get; private set; } = ManifestStatus.Pending;
    public int TotalKanban { get; private set; }
    public int ScannedKanban { get; private set; }

    public string OrderType { get; private set; } = "ORG";
    public int TotalSkid { get; private set; } = 1;
    public string DockCode { get; private set; } = "-";

    public PickupOrderDetail? Detail { get; private set; }
    public ICollection<PickupOrderKanban> Kanbans { get; private set; } = [];

    private PickupOrderManifest() { }

    public static PickupOrderManifest Create(long detailId, string manifestNo, int totalKanban, string orderType = "ORG", int totalSkid = 1, string dockCode = "-")
        => new() { 
            PickupOrderDetailId = detailId, 
            ManifestNo = manifestNo, 
            TotalKanban = totalKanban,
            OrderType = orderType,
            TotalSkid = totalSkid,
            DockCode = dockCode
        };

    public void IncrementScanned()
    {
        ScannedKanban++;
        if (ScannedKanban >= TotalKanban)
            Status = ManifestStatus.Verified;
    }
}

public static class ManifestStatus
{
    public const string Pending  = "PENDING";
    public const string Verified = "VERIFIED";
    public const string Partial  = "PARTIAL";
}

/// <summary>
/// Individual kanban scanned during a pickup.
/// Schema: [job].[pickup_order_kanbans]
/// </summary>
public sealed class PickupOrderKanban : AuditableEntity
{
    public long Id { get; private set; }
    public long PickupOrderManifestId { get; private set; }
    public string KanbanCode { get; private set; } = default!;
    public string Status { get; private set; } = "SCANNED";
    public DateTime ScannedAt { get; private set; }

    public PickupOrderManifest? Manifest { get; private set; }

    private PickupOrderKanban() { }

    public static PickupOrderKanban Create(long manifestId, string kanbanCode)
        => new() { PickupOrderManifestId = manifestId, KanbanCode = kanbanCode, ScannedAt = DateTime.UtcNow };
}
