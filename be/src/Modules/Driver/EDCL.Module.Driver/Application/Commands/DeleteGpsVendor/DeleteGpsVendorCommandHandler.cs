using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Driver.Application.Commands.DeleteGpsVendor;

internal sealed class DeleteGpsVendorCommandHandler(DriverDbContext dbContext)
    : IRequestHandler<DeleteGpsVendorCommand, Result>
{
    public async Task<Result> Handle(DeleteGpsVendorCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.GpsVendors
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity is null)
            return Result.Failure(Error.NotFound("GpsVendor.NotFound", "GPS Vendor not found."));

        entity.SoftDelete("SYSTEM");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
