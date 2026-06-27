namespace EDCL.Shared.Kernel.Events;

public class ManifestDeliveredIntegrationEvent
{
    public long ManifestId { get; set; }
    public string ManifestNo { get; set; } = string.Empty;
    public string Status { get; set; } = "Delivered";
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
    public string Remarks { get; set; } = string.Empty;
}
