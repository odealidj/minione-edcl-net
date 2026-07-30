namespace EDCL.Module.Driver.Application.DTOs;

public sealed record GpsVendorDto(
    long Id,
    string Code,
    string Name,
    int ProviderType,
    string? ApiUrl,
    string? ApiUsername,
    string? ApiToken);
