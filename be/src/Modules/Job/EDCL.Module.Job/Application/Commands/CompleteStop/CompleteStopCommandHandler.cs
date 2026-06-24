using EDCL.Module.Job.Application.Ports;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.CompleteStop;

public sealed class CompleteStopCommandHandler(
    IPickupOrderRepository repository) : IRequestHandler<CompleteStopCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CompleteStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await repository.GetStopByIdWithKanbansAsync(request.StopId, cancellationToken);
        if (stop == null)
            return Error.NotFound("Stop", request.StopId);

        if (stop.PickupOrder == null || stop.PickupOrder.DriverId != request.DriverId)
            return Error.Unauthorized("Stop.Unauthorized", "You are not assigned to this stop.");

        // Check if all manifests are verified?
        // Let's assume business rule: Stop can be picked up even if partial, but ideally all verified.
        // For now, just mark picked up.
        stop.MarkPickedUp();

        await repository.UpdateStopAsync(stop, cancellationToken);

        return true;
    }
}
