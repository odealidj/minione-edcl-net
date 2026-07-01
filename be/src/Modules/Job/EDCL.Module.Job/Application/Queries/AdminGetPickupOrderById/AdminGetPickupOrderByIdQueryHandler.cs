using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Queries.AdminGetPickupOrderById;

internal sealed class AdminGetPickupOrderByIdQueryHandler(JobDbContext dbContext) 
    : IRequestHandler<AdminGetPickupOrderByIdQuery, Result<AdminPickupOrderDto>>
{
    public async Task<Result<AdminPickupOrderDto>> Handle(AdminGetPickupOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var pickupOrder = await dbContext.PickupOrders
            .AsNoTracking()
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (pickupOrder == null)
            return Result<AdminPickupOrderDto>.Failure(Error.NotFound("PickupOrder.NotFound", "Pickup order not found"));

        var dto = new AdminPickupOrderDto(
            pickupOrder.Id, pickupOrder.DriverId, pickupOrder.TruckId, pickupOrder.PoNo, pickupOrder.PickupDate, 
            pickupOrder.RouteCode, pickupOrder.CycleCode, pickupOrder.EstimatedDepartureTime, pickupOrder.Status, pickupOrder.StartedAt, pickupOrder.CompletedAt,
            pickupOrder.Details.OrderBy(d => d.Sequence).Select(d => new AdminPickupOrderDetailDto(
                d.Id, d.SupplierId, d.Sequence, d.Status, d.ArrivedAt, d.PickedUpAt,
                new List<AdminPickupOrderManifestDto>()
            )).ToList()
        );

        return Result<AdminPickupOrderDto>.Success(dto);
    }
}
