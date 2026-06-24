using EDCL.Module.Auth.Application.Ports;
using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.UpdateFcmToken;

public sealed record UpdateFcmTokenCommand(long DriverId, string FcmToken)
    : ICommand<Result<bool>>;

public sealed class UpdateFcmTokenCommandHandler(
    IDriverRepository driverRepository,
    ICachePort cache)
    : IRequestHandler<UpdateFcmTokenCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        UpdateFcmTokenCommand request,
        CancellationToken cancellationToken)
    {
        var driver = await driverRepository.FindByIdAsync(request.DriverId, cancellationToken);
        if (driver is null)
            return Error.NotFound("Driver", request.DriverId);

        driver.UpdateFcmToken(request.FcmToken);
        await driverRepository.UpdateAsync(driver, cancellationToken);
        await driverRepository.SaveChangesAsync(cancellationToken);

        // Invalidate driver profile cache
        await cache.RemoveAsync(CacheKeys.DriverProfile(request.DriverId), cancellationToken);

        return true;
    }
}
