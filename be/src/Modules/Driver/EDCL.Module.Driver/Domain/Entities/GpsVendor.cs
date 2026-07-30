using EDCL.Shared.Kernel.Domain;
using EDCL.Module.Driver.Domain.Enums;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Represents a master GPS Vendor entity.
/// Schema: [driver].[gps_vendors]
/// </summary>
public sealed class GpsVendor : AuditableEntity
{
    public long Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public GpsProviderType ProviderType { get; private set; } = GpsProviderType.None;
    
    public string? ApiUrl { get; private set; }
    public string? ApiUsername { get; private set; }
    public string? ApiPassword { get; private set; }
    public string? ApiToken { get; private set; }

    public ICollection<LogisticPartnerGpsVendor> LogisticPartnerMappings { get; private set; } = [];

    private GpsVendor() { }

    public static GpsVendor Create(string code, string name, GpsProviderType providerType, string? apiUrl, string? apiUsername, string? apiPassword, string? apiToken)
        => new()
        {
            Code = code,
            Name = name,
            ProviderType = providerType,
            ApiUrl = apiUrl,
            ApiUsername = apiUsername,
            ApiPassword = apiPassword,
            ApiToken = apiToken
        };

    public void Update(string code, string name, GpsProviderType providerType, string? apiUrl, string? apiUsername, string? apiPassword, string? apiToken)
    {
        Code = code;
        Name = name;
        ProviderType = providerType;
        ApiUrl = apiUrl;
        ApiUsername = apiUsername;
        ApiPassword = apiPassword;
        ApiToken = apiToken;
    }
}
