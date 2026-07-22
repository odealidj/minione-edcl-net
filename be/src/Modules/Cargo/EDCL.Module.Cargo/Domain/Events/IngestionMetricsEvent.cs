namespace EDCL.Module.Cargo.Domain.Events;

using System;
public record IngestionMetricsEvent
{
    public long SessionId { get; init; }
    public DateTime SessionDate { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public int TotalProcessed { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public string Status { get; init; } = string.Empty;
}
