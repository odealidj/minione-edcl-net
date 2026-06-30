using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Commands.AdminCreatePickupOrder;

internal sealed class AdminCreatePickupOrderCommandHandler(JobDbContext dbContext) 
    : IRequestHandler<AdminCreatePickupOrderCommand, Result<long>>
{
    public async Task<Result<long>> Handle(AdminCreatePickupOrderCommand request, CancellationToken cancellationToken)
    {
        if (await dbContext.PickupOrders.AnyAsync(x => x.PoNo == request.PoNo, cancellationToken))
            return Result<long>.Failure(Error.Conflict("PickupOrder.Duplicate", $"PO No '{request.PoNo}' already exists."));

        var pickupOrder = PickupOrder.Create(request.DriverId, request.TruckId, request.PoNo, request.PickupDate, request.RouteCode, request.CycleCode, request.EstimatedDepartureTime);

        foreach (var stopDto in request.Stops)
        {
            var stop = PickupOrderDetail.Create(pickupOrder.Id, stopDto.SupplierId, stopDto.Sequence);
            
            foreach (var manifestDto in stopDto.Manifests)
            {
                var manifest = PickupOrderManifest.Create(stop.Id, manifestDto.ManifestNo, manifestDto.TotalKanban, manifestDto.OrderType, manifestDto.TotalSkid, manifestDto.DockCode);
                stop.Manifests.Add(manifest);
            }
            
            pickupOrder.Details.Add(stop);
        }

        dbContext.PickupOrders.Add(pickupOrder);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<long>.Success(pickupOrder.Id);
    }
}
