namespace EDCL.Module.Job.Application.Ports;

public interface IJobNotificationPort
{
    Task NotifyDriverJobStartedAsync(long driverId, long pickupOrderId, CancellationToken cancellationToken = default);
    Task NotifyDriverJobCompletedAsync(long driverId, long pickupOrderId, CancellationToken cancellationToken = default);
}
