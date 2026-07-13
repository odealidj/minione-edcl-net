using EDCL.Shared.Kernel.Common;
using MediatR;
using System;
using System.Collections.Generic;

namespace EDCL.Module.Job.Application.Commands.AdminCreatePickupOrder;

public sealed record AdminCreatePickupOrderCommand(
    long? DriverId,
    long? TruckId,
    DateTime PickupDate,
    string RouteCode,
    string CycleCode,
    TimeSpan EstimatedDepartureTime,
    List<AdminCreatePickupOrderDetailCommandDto> Stops
) : IRequest<Result<long>>;

public sealed record AdminCreatePickupOrderDetailCommandDto(
    long SupplierId,
    int Sequence,
    List<AdminCreatePickupOrderManifestCommandDto> Manifests
);

public sealed record AdminCreatePickupOrderManifestCommandDto(
    string ManifestNo,
    int TotalKanban,
    string OrderType,
    int TotalSkid,
    string DockCode
);
