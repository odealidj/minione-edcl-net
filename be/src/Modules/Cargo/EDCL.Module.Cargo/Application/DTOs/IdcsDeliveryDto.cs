namespace EDCL.Module.Cargo.Application.DTOs;

public class IdcsDeliveryDto
{
    public long Id { get; set; }
    public long? ManifestId { get; set; }
    public string? ManifestNo { get; set; }
    public string? Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Remarks { get; set; }
}
