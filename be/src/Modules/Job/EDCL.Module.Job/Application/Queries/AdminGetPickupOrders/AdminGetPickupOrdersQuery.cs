using EDCL.Shared.Kernel.Common;
using MediatR;
using System;
using System.Collections.Generic;

namespace EDCL.Module.Job.Application.Queries.AdminGetPickupOrders;

public sealed record AdminGetPickupOrdersQuery(string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<AdminGetPickupOrdersResponse>>;

public sealed record AdminGetPickupOrdersResponse(
    IReadOnlyList<AdminPickupOrderListItemDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public sealed record AdminPickupOrderListItemDto(long Id, long? DriverId, long? TruckId, string PoNo, DateTime PickupDate, string RouteCode, string CycleCode, TimeSpan EstimatedDepartureTime, string Status, DateTime? StartedAt, DateTime? CompletedAt);
