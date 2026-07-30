using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Driver.Application.Commands.CreateGpsVendor;

internal sealed class CreateGpsVendorCommandHandler(DriverDbContext dbContext)
    : IRequestHandler<CreateGpsVendorCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateGpsVendorCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.GpsVendors
            .AnyAsync(x => x.Code == request.Code && !x.IsDeleted, cancellationToken);

        if (exists)
            return Result<long>.Failure(Error.Conflict("GpsVendor.DuplicateCode", $"GPS Vendor with code '{request.Code}' already exists."));

        var entity = EDCL.Module.Driver.Domain.Entities.GpsVendor.Create(
            request.Code,
            request.Name,
            (EDCL.Module.Driver.Domain.Enums.GpsProviderType)request.ProviderType,
            request.ApiUrl,
            request.ApiUsername,
            request.ApiPassword,
            request.ApiToken);

        dbContext.GpsVendors.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(entity.Id);
    }
}
