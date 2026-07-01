namespace EDCL.Module.Cargo.Application.DTOs;

public sealed record AdminManifestDto(long Id, string ManifestNo, string SupplierCode, string SupplierName, int TotalKanbans, int TotalParts, string Status);
