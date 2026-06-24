using EDCL.Module.Job.Domain.Entities;

namespace EDCL.Module.Job.Application.Ports;

public interface IPickupOrderRepository
{
    Task<PickupOrder?> GetCurrentJobAsync(long driverId, CancellationToken cancellationToken = default);
    Task<PickupOrder?> GetNextJobAsync(long driverId, CancellationToken cancellationToken = default);
    Task<PickupOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PickupOrder?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default);
    Task<PickupOrderDetail?> GetStopByIdWithKanbansAsync(long stopId, CancellationToken cancellationToken = default);
    Task UpdateAsync(PickupOrder pickupOrder, CancellationToken cancellationToken = default);
    Task UpdateStopAsync(PickupOrderDetail stop, CancellationToken cancellationToken = default);
}
