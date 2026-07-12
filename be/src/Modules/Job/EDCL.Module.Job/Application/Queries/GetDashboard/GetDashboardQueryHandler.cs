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
            LogisticPartnerName: driver.LogisticPartnerName);

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

        // Format Time exactly as mockup (e.g. "01 Feb 2024, 01:00")
        var formattedTime = $"{job.PickupDate:dd MMM yyyy}, {job.EstimatedDepartureTime:hh\\:mm}";

        return new JobCardDto(
            PickupOrderId: job.Id,
            RouteCode: job.RouteCode,
            Cycle: job.CycleCode,
            DeliveryNo: job.PoNo,
            PickupDate: job.PickupDate.ToString("dd MMM yyyy"),
            Time: formattedTime,
            TruckPlate: truckPlate);
    }
}
