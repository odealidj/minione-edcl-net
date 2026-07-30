namespace EDCL.Module.Driver.Application.DTOs;

public sealed record TruckDto(long Id, string PlateNumber, string? VehicleType, long LogisticPartnerId, string? LogisticPartnerName, bool IsActive, bool IsSimulated, string? GpsVehicleId);
