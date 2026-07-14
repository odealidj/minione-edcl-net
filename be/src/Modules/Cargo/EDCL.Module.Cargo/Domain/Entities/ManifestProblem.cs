namespace EDCL.Module.Cargo.Domain.Entities;

using EDCL.Shared.Kernel.Domain;
using System;

public class ManifestProblem : AuditableEntity
{
    public long Id { get; private set; }
    
    /// <summary>
    /// The CDC operation that caused the problem (e.g., "UPDATE", "DELETE")
    /// </summary>
    public string OperationType { get; private set; } = default!;
    
    /// <summary>
    /// The raw CDC payload
    /// </summary>
    public string Payload { get; private set; } = default!;
    
    /// <summary>
    /// Problem description indicating why the CDC event was rejected.
    /// </summary>
    public string Description { get; private set; } = default!;
    
    /// <summary>
    /// Status of this problem record (e.g., "Pending Resolution", "Ignored", "Resolved").
    /// </summary>
    public string Status { get; private set; } = "Pending Resolution";
    
    public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

    public string? ManifestNo { get; private set; }
    
    public string? DeliveryNo { get; private set; }
    
    public long? PickupOrderId { get; private set; }
    
    public string? ResolvedBy { get; private set; }
    
    public DateTime? ResolvedAt { get; private set; }
    
    public string? ResolutionReason { get; private set; }

    protected ManifestProblem() { }

    public ManifestProblem(string operationType, string payload, string description, string? manifestNo = null, string? deliveryNo = null, long? pickupOrderId = null)
    {
        OperationType = operationType;
        Payload = payload;
        Description = description;
        ManifestNo = manifestNo;
        DeliveryNo = deliveryNo;
        PickupOrderId = pickupOrderId;
        OccurredAt = DateTime.UtcNow;
    }

    public void Resolve(string newStatus, string resolvedBy, string reason)
    {
        Status = newStatus;
        ResolvedBy = resolvedBy;
        ResolutionReason = reason;
        ResolvedAt = DateTime.UtcNow;
    }
}
