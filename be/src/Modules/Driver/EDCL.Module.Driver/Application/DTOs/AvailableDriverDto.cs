namespace EDCL.Module.Driver.Application.DTOs;

public sealed record AvailableDriverDto(
    long Id,
    string Name,
    string Nik,
    string PhoneNumber
);
