namespace EDCL.Module.Driver.Application.DTOs;

public sealed record SupplierDto(long Id, string SupplierCode, string Name, string? Address, double? Latitude, double? Longitude, int? GeofenceRadiusMeters, bool IsActive);
