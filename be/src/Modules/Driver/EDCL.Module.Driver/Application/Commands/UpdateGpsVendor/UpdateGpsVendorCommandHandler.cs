using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Driver.Application.Commands.UpdateGpsVendor;

internal sealed class UpdateGpsVendorCommandHandler(DriverDbContext dbContext)
    : IRequestHandler<UpdateGpsVendorCommand, Result>
{
    public async Task<Result> Handle(UpdateGpsVendorCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.GpsVendors
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity is null)
            return Result.Failure(Error.NotFound("GpsVendor.NotFound", "GPS Vendor not found."));

        entity.Update(
            request.Code,
            request.Name,
            (EDCL.Module.Driver.Domain.Enums.GpsProviderType)request.ProviderType,
            request.ApiUrl,
            request.ApiUsername,
            request.ApiPassword,
            request.ApiToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
