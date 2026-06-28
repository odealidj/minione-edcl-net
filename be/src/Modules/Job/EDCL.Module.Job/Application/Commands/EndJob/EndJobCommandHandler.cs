using EDCL.Module.Job.Application.Ports;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.EndJob;

public sealed class EndJobCommandHandler(
    IPickupOrderRepository repository,
    IJobNotificationPort notificationPort) : IRequestHandler<EndJobCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(EndJobCommand request, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdWithDetailsAsync(request.PickupOrderId, cancellationToken);
        if (job == null)
            return Error.NotFound("Job.NotFound", "Pickup order not found.");

        if (job.DriverId != request.DriverId)
            return Error.Unauthorized("Job.Unauthorized", "You are not assigned to this pickup order.");

        // Additional geofence validation could happen here using request.Latitude/Longitude

        try
        {
            job.Complete();
            await repository.UpdateAsync(job, cancellationToken);

            // Notify driver/system
            await notificationPort.NotifyDriverJobCompletedAsync(request.DriverId, job.Id, cancellationToken);

            return true;
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("Job.InvalidState", ex.Message);
        }
    }
}
