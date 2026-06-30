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
            .Include(x => x.Details).ThenInclude(x => x.Manifests).ThenInclude(x => x.Kanbans)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (pickupOrder == null)
            return Result<AdminPickupOrderDto>.Failure(Error.NotFound("PickupOrder.NotFound", "Pickup order not found"));

        var dto = new AdminPickupOrderDto(
            pickupOrder.Id, pickupOrder.DriverId, pickupOrder.TruckId, pickupOrder.PoNo, pickupOrder.PickupDate, 
            pickupOrder.RouteCode, pickupOrder.CycleCode, pickupOrder.EstimatedDepartureTime, pickupOrder.Status, pickupOrder.StartedAt, pickupOrder.CompletedAt,
            pickupOrder.Details.OrderBy(d => d.Sequence).Select(d => new AdminPickupOrderDetailDto(
                d.Id, d.SupplierId, d.Sequence, d.Status, d.ArrivedAt, d.PickedUpAt,
                d.Manifests.Select(m => new AdminPickupOrderManifestDto(
                    m.Id, m.ManifestNo, m.Status, m.TotalKanban, m.ScannedKanban, m.OrderType, m.TotalSkid, m.DockCode,
                    m.Kanbans.Select(k => new AdminPickupOrderKanbanDto(
                        k.Id, k.KanbanCode, k.Status, k.ScannedAt
                    )).ToList()
                )).ToList()
            )).ToList()
        );

        return Result<AdminPickupOrderDto>.Success(dto);
    }
}
