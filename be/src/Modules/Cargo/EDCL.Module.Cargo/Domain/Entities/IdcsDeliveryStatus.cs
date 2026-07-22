namespace EDCL.Module.Cargo.Domain.Entities;

public class IdcsDeliveryStatus
{
    public long Id { get; private set; }
    public long? ManifestId { get; private set; }
    public string? ManifestNo { get; private set; }
    public string? Status { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? Remarks { get; private set; }
}
