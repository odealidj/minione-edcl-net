using EDCL.Module.Job.Application.Ports;
using EDCL.Module.Job.Domain.Entities;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler(
    IPickupOrderRepository pickupOrderRepository,
    IDriverPort driverPort,
    ITruckPort truckPort) : IRequestHandler<GetDashboardQuery, Result<DashboardResponse>>
{
    public async Task<Result<DashboardResponse>> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        // 1. Get driver profile
        var driver = await driverPort.GetActiveDriverByIdAsync(request.DriverId, cancellationToken);
        if (driver == null)
            return Error.NotFound("Driver.NotFound", "Active driver not found.");

        var profileDto = new DriverProfileDto(
            Name: driver.Name,
            PhotoUrl: driver.PhotoUrl,
            TransporterName: driver.TransporterName);

        // 2. Get current job & next job
        var currentJob = await pickupOrderRepository.GetCurrentJobAsync(request.DriverId, cancellationToken);
        var nextJob = await pickupOrderRepository.GetNextJobAsync(request.DriverId, cancellationToken);

        var currentJobDto = await MapToJobCardDtoAsync(currentJob, cancellationToken);
        var nextJobDto = await MapToJobCardDtoAsync(nextJob, cancellationToken);

        return new DashboardResponse(profileDto, currentJobDto, nextJobDto);
    }

    private async Task<JobCardDto?> MapToJobCardDtoAsync(PickupOrder? job, CancellationToken cancellationToken)
    {
        if (job == null) return null;

        var truckPlate = "-";
        if (job.TruckId.HasValue)
        {
            var truck = await truckPort.GetTruckByIdAsync(job.TruckId.Value, cancellationToken);
            truckPlate = truck?.PlateNumber ?? "-";
        }

        // Mocking RouteCode and Cycle from PoNo for now if not strictly defined.
        // Assuming PoNo format: R202402010081. Let's just use placeholder for RouteCode/Cycle.
        var routeCode = "RD23";
        var cycle = "01";

        return new JobCardDto(
            PickupOrderId: job.Id,
            RouteCode: routeCode,
            Cycle: cycle,
            DeliveryNo: job.PoNo,
            PickupDate: job.CreatedAt.ToString("dd MMM yyyy"),
            Time: job.CreatedAt.ToString("dd MMM yyyy, HH:mm"),
            TruckPlate: truckPlate);
    }
}
