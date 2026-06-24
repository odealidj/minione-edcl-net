using EDCL.Module.Job.Application.Ports;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.GetRouteStops;

public sealed class GetRouteStopsQueryHandler(
    IPickupOrderRepository repository,
    ISupplierPort supplierPort) : IRequestHandler<GetRouteStopsQuery, Result<RouteStopsResponse>>
{
    public async Task<Result<RouteStopsResponse>> Handle(GetRouteStopsQuery request, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdWithDetailsAsync(request.PickupOrderId, cancellationToken);
        if (job == null)
            return Error.NotFound("Job.NotFound", "Pickup order not found.");

        if (job.DriverId != request.DriverId)
            return Error.Unauthorized("Job.Unauthorized", "You are not assigned to this pickup order.");

        var stops = new List<RouteStopDto>();
        foreach (var detail in job.Details)
        {
            var supplier = await supplierPort.GetSupplierByIdAsync(detail.SupplierId, cancellationToken);
            
            // Calculate scanned vs total for this stop's manifests.
            // Since we don't have manifest parts mapped fully yet, we just aggregate the manifests' TotalKanban and ScannedKanban
            int totalKanban = detail.Manifests.Sum(x => x.TotalKanban);
            int scannedKanban = detail.Manifests.Sum(x => x.ScannedKanban);

            stops.Add(new RouteStopDto(
                StopId: detail.Id,
                Sequence: detail.Sequence,
                Label: GetOrdinal(detail.Sequence) + " Pickup",
                Status: detail.Status,
                SupplierName: supplier?.Name ?? "Unknown Supplier",
                SupplierCode: supplier?.SupplierCode ?? "Unknown",
                Eta: detail.ArrivedAt?.ToString("HH:mm") ?? "-",
                Etd: detail.PickedUpAt?.ToString("HH:mm") ?? "-",
                Original: new ManifestCounterDto(scannedKanban, totalKanban),
                Others: new ManifestCounterDto(0, 0), // As per instruction: ignore other order types
                Eo: new ManifestCounterDto(0, 0)
            ));
        }

        return new RouteStopsResponse(
            RouteCode: "RD23", // Mock for now
            Cycle: "01",       // Mock for now
            DeliveryNo: job.PoNo,
            Date: job.CreatedAt.ToString("dd MMM yyyy"),
            Stops: stops
        );
    }

    private static string GetOrdinal(int number)
    {
        if (number <= 0) return number.ToString();
        switch (number % 100)
        {
            case 11:
            case 12:
            case 13:
                return number + "th";
        }
        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th",
        };
    }
}
