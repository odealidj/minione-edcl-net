using System;
using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

/// <summary>
/// Represents the pricing scheme for a specific Route and Logistic Partner.
/// Schema: [driver].[route_prices]
/// Legacy: TB_M_ROUTE_PRICE
/// </summary>
public sealed class RoutePrice : AuditableEntity
{
    public long Id { get; private set; }
    public long RouteId { get; private set; }
    public long LogisticPartnerId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public string PriceType { get; private set; } = default!;

    // Navigation properties
    public Route Route { get; private set; } = default!;
    public LogisticPartner LogisticPartner { get; private set; } = default!;

    private RoutePrice() { }

    public static RoutePrice Create(
        long routeId,
        long logisticPartnerId,
        decimal price,
        DateTime validFrom,
        DateTime validTo,
        string priceType)
    {
        return new RoutePrice
        {
            RouteId = routeId,
            LogisticPartnerId = logisticPartnerId,
            Price = price,
            ValidFrom = validFrom,
            ValidTo = validTo,
            PriceType = priceType
        };
    }

    public void Update(
        long routeId,
        long logisticPartnerId,
        decimal price,
        DateTime validFrom,
        DateTime validTo,
        string priceType)
    {
        RouteId = routeId;
        LogisticPartnerId = logisticPartnerId;
        Price = price;
        ValidFrom = validFrom;
        ValidTo = validTo;
        PriceType = priceType;
    }
}
