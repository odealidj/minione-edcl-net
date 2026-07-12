namespace EDCL.Module.Auth.Application.DTOs;

public sealed record DriverDto(long Id, string Name, string Nik, string PhoneNumber, bool IsActive, long? LogisticPartnerId);
