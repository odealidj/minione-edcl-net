using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Mapping table for Many-to-Many relation between LogisticPartner and GpsVendor.
/// Schema: [driver].[logistic_partner_gps_vendors]
/// </summary>
public sealed class LogisticPartnerGpsVendor : AuditableEntity
{
    public long LogisticPartnerId { get; private set; }
    public LogisticPartner LogisticPartner { get; private set; } = default!;

    public long GpsVendorId { get; private set; }
    public GpsVendor GpsVendor { get; private set; } = default!;

    // Sync status is stored in the mapping, because a Logistic Partner could have multiple vendors syncing
    public DateTime? LastGpsSyncAt { get; private set; }
    public string? LastGpsSyncStatus { get; private set; }
    public string? LastGpsSyncMessage { get; private set; }
    public DateTime? NextGpsSyncAt { get; private set; }

    private LogisticPartnerGpsVendor() { }

    public static LogisticPartnerGpsVendor Create(long logisticPartnerId, long gpsVendorId)
        => new()
        {
            LogisticPartnerId = logisticPartnerId,
            GpsVendorId = gpsVendorId
        };

    public void UpdateSyncStatus(DateTime syncAt, string status, string? message = null)
    {
        LastGpsSyncAt = syncAt;
        LastGpsSyncStatus = status;
        LastGpsSyncMessage = message;
    }

    public void SetNextSyncTime(DateTime nextSyncAt)
    {
        NextGpsSyncAt = nextSyncAt;
    }
}
