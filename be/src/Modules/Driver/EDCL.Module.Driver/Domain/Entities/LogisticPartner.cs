using EDCL.Shared.Kernel.Domain;
using EDCL.Module.Driver.Domain.Enums;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Represents a logistic partner company that provides trucks and drivers.
/// Schema: [driver].[logistic_partners]
/// </summary>
public sealed class LogisticPartner : AuditableEntity
{
    public long Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    public ICollection<Truck> Trucks { get; private set; } = [];
    public ICollection<LogisticPartnerGpsVendor> GpsVendorMappings { get; private set; } = [];

    private LogisticPartner() { }

    public static LogisticPartner Create(string code, string name)
        => new()
        {
            Code = code,
            Name = name
        };

    public void Update(string code, string name)
    {
        Code = code;
        Name = name;
    }
}
