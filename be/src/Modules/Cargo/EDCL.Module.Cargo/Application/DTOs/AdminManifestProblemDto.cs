namespace EDCL.Module.Cargo.Application.DTOs;

public sealed record AdminManifestProblemDto(
    long Id,
    string OperationType,
    string Payload,
    string Description,
    string Status,
    System.DateTime OccurredAt,
    string? ManifestNo,
    string? DeliveryNo,
    long? PickupOrderId,
    string? ResolvedBy,
    System.DateTime? ResolvedAt,
    string? ResolutionReason
);
