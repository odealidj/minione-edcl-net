using EDCL.Module.Job.Application.Ports;
using EDCL.Module.Job.Domain.Entities;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.ScanKanban;

public sealed class ScanKanbanCommandHandler(
    IPickupOrderRepository repository) : IRequestHandler<ScanKanbanCommand, Result<ScanKanbanResponse>>
{
    public async Task<Result<ScanKanbanResponse>> Handle(ScanKanbanCommand request, CancellationToken cancellationToken)
    {
        var stop = await repository.GetStopByIdWithKanbansAsync(request.StopId, cancellationToken);
        if (stop == null)
            return Error.NotFound("Stop", request.StopId);

        if (stop.PickupOrder == null || stop.PickupOrder.DriverId != request.DriverId)
            return Error.Unauthorized("Stop.Unauthorized", "You are not assigned to this stop.");

        var manifest = stop.Manifests.FirstOrDefault(x => x.Id == request.ManifestId);
        if (manifest == null)
            return Error.NotFound("Manifest", request.ManifestId);

        if (manifest.Kanbans.Any(x => x.KanbanCode == request.KanbanCode))
        {
            // Idempotency support for rapid scanning: return success instead of error
            return new ScanKanbanResponse(
                ManifestId: manifest.Id,
                Status: manifest.Status,
                Scanned: manifest.ScannedKanban,
                Total: manifest.TotalKanban);
        }

        // Create kanban record
        var kanban = PickupOrderKanban.Create(manifest.Id, request.KanbanCode);
        manifest.Kanbans.Add(kanban);

        // Update counts
        manifest.IncrementScanned();

        // Save
        await repository.UpdateStopAsync(stop, cancellationToken);

        return new ScanKanbanResponse(
            ManifestId: manifest.Id,
            Status: manifest.Status,
            Scanned: manifest.ScannedKanban,
            Total: manifest.TotalKanban);
    }
}
