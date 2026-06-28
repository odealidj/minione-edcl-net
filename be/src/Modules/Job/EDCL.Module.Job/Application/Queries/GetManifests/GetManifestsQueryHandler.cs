using EDCL.Module.Job.Application.Ports;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;
using MediatR;
using EDCL.Module.Job.Domain.Entities;

namespace EDCL.Module.Job.Application.Queries.GetManifests;

public sealed class GetManifestsQueryHandler(
    IPickupOrderRepository repository,
    ISupplierPort supplierPort) : IRequestHandler<GetManifestsQuery, Result<ManifestListResponse>>
{
    public async Task<Result<ManifestListResponse>> Handle(GetManifestsQuery request, CancellationToken cancellationToken)
    {
        var stop = await repository.GetStopByIdWithKanbansAsync(request.StopId, cancellationToken);
        if (stop == null)
            return Error.NotFound("Stop.NotFound", "Pickup order stop not found.");

        if (stop.PickupOrder == null || stop.PickupOrder.DriverId != request.DriverId)
            return Error.Unauthorized("Stop.Unauthorized", "You are not assigned to this pickup order.");

        var supplier = await supplierPort.GetSupplierByIdAsync(stop.SupplierId, cancellationToken);

        var manifests = stop.Manifests
            .OrderBy(m => m.ManifestNo)
            .Select(m => new ManifestDto(
                ManifestNo: m.ManifestNo,
                Type: m.OrderType,
                TotalSkid: m.TotalSkid,
                DockCode: m.DockCode,
                NoOfKanban: m.TotalKanban,
                ScannedKanban: m.ScannedKanban,
                IsComplete: m.Status == ManifestStatus.Verified
            )).ToList();

        return new ManifestListResponse(
            SupplierName: supplier?.Name ?? "Unknown Supplier",
            SupplierCode: supplier?.SupplierCode ?? "Unknown",
            Manifests: manifests
        );
    }
}
