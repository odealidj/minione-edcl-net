namespace EDCL.Module.Cargo.Domain.Entities;

using System;
using EDCL.Shared.Kernel.Domain;

public class SyncSession : AuditableEntity
{
    public long Id { get; set; }
    public DateTime SessionDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string Status { get; set; } = string.Empty; // "IN_PROGRESS" or "COMPLETED"
    public string? EventBreakdown { get; set; } // JSON formatted breakdown of events
}
