using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Represents a logistics route and cycle for rate calculation.
/// Schema: [driver].[routes]
/// </summary>
public sealed class Route : AuditableEntity
{
    public long Id { get; private set; }
    public string RouteCode { get; private set; } = default!;
    public string CycleCode { get; private set; } = default!;

    private Route() { }

    public static Route Create(string routeCode, string cycleCode)
    {
        return new Route
        {
            RouteCode = routeCode,
            CycleCode = cycleCode
        };
    }

    public void Update(string routeCode, string cycleCode)
    {
        RouteCode = routeCode;
        CycleCode = cycleCode;
    }
}
