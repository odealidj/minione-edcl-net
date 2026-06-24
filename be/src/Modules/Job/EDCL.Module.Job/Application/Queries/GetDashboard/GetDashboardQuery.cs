using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.GetDashboard;

public sealed record GetDashboardQuery(long DriverId) : IRequest<Result<DashboardResponse>>;

public sealed record DashboardResponse(
    DriverProfileDto Profile,
    JobCardDto? CurrentJob,
    JobCardDto? NextJob);

public sealed record DriverProfileDto(
    string Name,
    string? PhotoUrl,
    string? TransporterName);

public sealed record JobCardDto(
    long PickupOrderId,
    string RouteCode,
    string Cycle,
    string DeliveryNo,
    string Time,
    string TruckPlate);
