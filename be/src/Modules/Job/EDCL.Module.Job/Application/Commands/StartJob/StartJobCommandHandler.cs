using EDCL.Module.Job.Application.Ports;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.StartJob;

public sealed class StartJobCommandHandler(
    IPickupOrderRepository repository,
    IJobNotificationPort notificationPort) : IRequestHandler<StartJobCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(StartJobCommand request, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(request.PickupOrderId, cancellationToken);
        if (job == null)
            return Error.NotFound("Job.NotFound", "Pickup order not found.");

        if (job.DriverId != request.DriverId)
            return Error.Unauthorized("Job.Unauthorized", "You are not assigned to this pickup order.");

        try
        {
            job.Start();
            await repository.UpdateAsync(job, cancellationToken);
            
            // Notify driver
            await notificationPort.NotifyDriverJobStartedAsync(request.DriverId, job.Id, cancellationToken);

            return true;
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("Job.InvalidState", ex.Message);
        }
    }
}
