namespace EDCL.Module.Cargo.Domain.Entities;

public class IngestionError
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool IsResolved { get; set; }
}
