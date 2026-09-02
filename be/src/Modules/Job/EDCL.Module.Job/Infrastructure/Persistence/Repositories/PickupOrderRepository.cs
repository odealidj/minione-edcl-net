using EDCL.Module.Job.Application.Ports;
using EDCL.Module.Job.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Job.Infrastructure.Persistence.Repositories;

public sealed class PickupOrderRepository(JobDbContext dbContext) : IPickupOrderRepository
{
    public async Task<PickupOrder?> GetCurrentJobAsync(long driverId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PickupOrders
            .Include(x => x.Details.OrderBy(d => d.Sequence))
            .Where(x => x.DriverId == driverId && x.Status == PickupOrderStatus.OnProgress)
            .OrderBy(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PickupOrder?> GetNextJobAsync(long driverId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PickupOrders
            .Where(x => x.DriverId == driverId && x.Status == PickupOrderStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PickupOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await dbContext.PickupOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PickupOrder?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await dbContext.PickupOrders
            .Include(x => x.Details.OrderBy(d => d.Sequence))
                .ThenInclude(d => d.Manifests)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PickupOrderDetail?> GetStopByIdWithKanbansAsync(long stopId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PickupOrderDetails
            .Include(x => x.Manifests)
                .ThenInclude(m => m.Kanbans)
            .Include(x => x.PickupOrder)
            .FirstOrDefaultAsync(x => x.Id == stopId, cancellationToken);
    }

    public async Task UpdateAsync(PickupOrder pickupOrder, CancellationToken cancellationToken = default)
    {
        dbContext.PickupOrders.Update(pickupOrder);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStopAsync(PickupOrderDetail stop, CancellationToken cancellationToken = default)
    {
        dbContext.PickupOrderDetails.Update(stop);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
