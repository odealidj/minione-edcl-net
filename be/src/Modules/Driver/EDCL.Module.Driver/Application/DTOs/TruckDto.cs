namespace EDCL.Module.Driver.Application.DTOs;

public sealed record TruckDto(long Id, string PlateNumber, string? VehicleType, bool IsActive);
