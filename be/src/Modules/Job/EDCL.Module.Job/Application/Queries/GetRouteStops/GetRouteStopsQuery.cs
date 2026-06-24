using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.GetRouteStops;

public sealed record GetRouteStopsQuery(long PickupOrderId, long DriverId) : IRequest<Result<RouteStopsResponse>>;

public sealed record RouteStopsResponse(
    string RouteCode,
    string Cycle,
    string DeliveryNo,
    string Date,
    List<RouteStopDto> Stops);

public sealed record RouteStopDto(
    long StopId,
    int Sequence,
    string Label,
    string Status,
    string SupplierName,
    string SupplierCode,
    string Eta,
    string Etd,
    ManifestCounterDto Original,
    ManifestCounterDto Others,
    ManifestCounterDto Eo);

public sealed record ManifestCounterDto(
    int Scanned,
    int Total);
