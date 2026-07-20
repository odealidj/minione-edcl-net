using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Events;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Commands.AdminDeletePickupOrder;

internal sealed class AdminDeletePickupOrderCommandHandler(JobDbContext dbContext, IMediator mediator) 
    : IRequestHandler<AdminDeletePickupOrderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AdminDeletePickupOrderCommand request, CancellationToken cancellationToken)
    {
        var pickupOrder = await dbContext.PickupOrders
            .Include(x => x.Details).ThenInclude(x => x.Manifests)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            
        if (pickupOrder == null) return Result<bool>.Failure(Error.NotFound("PickupOrder.NotFound", "Pickup order not found"));
        
        if (pickupOrder.Status != PickupOrderStatus.Pending)
            return Result<bool>.Failure(Error.Conflict("PickupOrder.InvalidStatus", "Cannot delete pickup order that is not PENDING."));
            
        var manifestNos = pickupOrder.Details.SelectMany(x => x.Manifests).Select(x => x.ManifestNo).ToList();
            
        // Delete scheduled hangfire jobs
        if (!string.IsNullOrEmpty(pickupOrder.HangfireJobIdH1))
            BackgroundJob.Delete(pickupOrder.HangfireJobIdH1);
            
        if (!string.IsNullOrEmpty(pickupOrder.HangfireJobIdH30))
            BackgroundJob.Delete(pickupOrder.HangfireJobIdH30);

        dbContext.PickupOrders.Remove(pickupOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        if (manifestNos.Any())
            await mediator.Publish(new ManifestsAssignedToRouteIntegrationEvent(manifestNos, false), cancellationToken);
            
        return Result<bool>.Success(true);
    }
}
