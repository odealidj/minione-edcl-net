using EDCL.Module.Job.Application.Ports;
using EDCL.Shared.Kernel.Common;
using MediatR;

using EDCL.Shared.Kernel.Ports;

namespace EDCL.Module.Job.Application.Commands.CompleteStop;

public sealed class CompleteStopCommandHandler(
    IPickupOrderRepository repository,
    ISupplierPort supplierPort) : IRequestHandler<CompleteStopCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(CompleteStopCommand request, CancellationToken cancellationToken)
    {
        var stop = await repository.GetStopByIdWithKanbansAsync(request.StopId, cancellationToken);
        if (stop == null)
            return Error.NotFound("Stop", request.StopId);

        if (stop.PickupOrder == null || stop.PickupOrder.DriverId != request.DriverId)
            return Error.Unauthorized("Stop.Unauthorized", "You are not assigned to this stop.");

        var supplier = await supplierPort.GetSupplierByIdAsync(stop.SupplierId, cancellationToken);
        if (supplier == null)
            return Error.NotFound("Supplier", stop.SupplierId);

        if (supplier.Latitude.HasValue && supplier.Longitude.HasValue)
        {
            var distanceMeters = CalculateDistance(request.Latitude, request.Longitude, supplier.Latitude.Value, supplier.Longitude.Value);
            var maxDistance = supplier.GeofenceRadiusMeters ?? 500;
            
            if (distanceMeters > maxDistance)
            {
                return Error.Validation("Geofence.OutOfRange", $"Anda berada di luar jangkauan radius {maxDistance} meter dari Supplier.");
            }
        }

        // Check if all manifests are verified?
        // Let's assume business rule: Stop can be picked up even if partial, but ideally all verified.
        // For now, just mark picked up.
        stop.MarkPickedUp();

        await repository.UpdateStopAsync(stop, cancellationToken);

        return true;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return 6371000 * c; // Earth radius in meters
    }
}
