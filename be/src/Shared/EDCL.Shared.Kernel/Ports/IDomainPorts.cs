namespace EDCL.Shared.Kernel.Ports;

// ── Cross-domain contracts ──────────────────────────────────────────────────
// These interfaces are defined in Shared.Kernel so that any module can depend
// on them WITHOUT creating a hard reference to another module's internals.
// Each module implementing a port registers its adapter via DI.

/// <summary>
/// Provides driver identity data to other modules (e.g., Job, Notification).
/// Implemented by EDCL.Module.Auth.
/// </summary>
public interface IDriverPort
{
    Task<DriverInfo?> GetActiveDriverByIdAsync(long driverId, CancellationToken ct = default);
    Task<IReadOnlyList<DriverInfo>> GetDriversByIdsAsync(IEnumerable<long> driverIds, CancellationToken ct = default);
    Task<IReadOnlyList<DriverInfo>> GetActiveDriversByLogisticPartnerIdAsync(long logisticPartnerId, CancellationToken ct = default);
}

/// <summary>
/// Provides supplier master data to other modules (e.g., Job, Cargo).
/// Implemented by EDCL.Module.Driver (master data module).
/// </summary>
public interface ISupplierPort
{
    Task<SupplierInfo?> GetSupplierByIdAsync(long supplierId, CancellationToken ct = default);
    Task<SupplierInfo?> GetSupplierByCodeAsync(string supplierCode, CancellationToken ct = default);
}

/// <summary>
/// Provides truck information to other modules.
/// Implemented by EDCL.Module.Driver.
/// </summary>
public interface ITruckPort
{
    Task<TruckInfo?> GetTruckByIdAsync(long truckId, CancellationToken ct = default);
}

// ── DTOs for cross-domain communication ─────────────────────────────────────

public sealed record DriverInfo(
    long Id,
    string Nik,
    string Name,
    string PhoneNumber,
    string? PhotoUrl,
    long? LogisticPartnerId,
    string? LogisticPartnerName,
    bool IsActive,
    string? FcmToken = null);

public sealed record SupplierInfo(
    long Id,
    string SupplierCode,
    string Name,
    string? Address,
    double? Latitude,
    double? Longitude,
    int? GeofenceRadiusMeters);

public sealed record TruckInfo(
    long Id,
    string PlateNumber,
    string? VehicleType);

/// <summary>
/// Provides notification status to other modules.
/// Implemented by EDCL.Module.Notification.
/// </summary>
public interface INotificationPort
{
    Task<IReadOnlyDictionary<long, NotificationStatusInfo>> GetNotificationStatusesByReferenceIdsAsync(
        IEnumerable<long> referenceIds, 
        string type, 
        CancellationToken ct = default);
}

public sealed record NotificationStatusInfo(
    long Id,
    string? FcmDeliveryStatus,
    string? FcmErrorMessage,
    bool IsRead);
