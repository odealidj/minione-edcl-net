using EDCL.Shared.Kernel.Common;
using MediatR;
using System;
using System.Collections.Generic;

namespace EDCL.Module.Job.Application.Commands.AdminUpdatePickupOrder;

public sealed record AdminUpdatePickupOrderCommand(
    long Id,
    long? DriverId,
    long? TruckId,
    DateTime PickupDate,
    string RouteCode,
    string CycleCode,
    TimeSpan EstimatedDepartureTime,
    List<AdminUpdatePickupOrderDetailCommandDto> Stops
) : IRequest<Result<bool>>;

public sealed record AdminUpdatePickupOrderDetailCommandDto(
    long Id, // 0 if new
    long SupplierId,
    int Sequence,
    List<AdminUpdatePickupOrderManifestCommandDto> Manifests
);

public sealed record AdminUpdatePickupOrderManifestCommandDto(
    long Id, // 0 if new
    string ManifestNo,
    int TotalKanban,
    string OrderType,
    int TotalSkid,
    string DockCode
);
