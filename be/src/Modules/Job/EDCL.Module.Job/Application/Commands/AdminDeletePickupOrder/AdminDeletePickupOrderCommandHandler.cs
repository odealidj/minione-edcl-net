using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Commands.AdminDeletePickupOrder;

internal sealed class AdminDeletePickupOrderCommandHandler(JobDbContext dbContext) 
    : IRequestHandler<AdminDeletePickupOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AdminDeletePickupOrderCommand request, CancellationToken cancellationToken)
    {
        var pickupOrder = await dbContext.PickupOrders.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (pickupOrder == null) return Result<bool>.Failure(Error.NotFound("PickupOrder.NotFound", "Pickup order not found"));
        
        if (pickupOrder.Status != PickupOrderStatus.Pending)
            return Result<bool>.Failure(Error.Conflict("PickupOrder.InvalidStatus", "Cannot delete pickup order that is not PENDING."));
            
        dbContext.PickupOrders.Remove(pickupOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
