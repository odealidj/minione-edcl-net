using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Queries.GetGpsVendorById;

internal sealed class GetGpsVendorByIdQueryHandler(DriverDbContext dbContext)
    : IRequestHandler<GetGpsVendorByIdQuery, Result<GpsVendorDto>>
{
    public async Task<Result<GpsVendorDto>> Handle(GetGpsVendorByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.GpsVendors
            .FirstOrDefaultAsync(v => v.Id == request.Id && !v.IsDeleted, cancellationToken);

        if (x is null)
            return Result<GpsVendorDto>.Failure(Error.NotFound("GpsVendor.NotFound", "GPS Vendor not found."));

        return Result<GpsVendorDto>.Success(new GpsVendorDto(
            x.Id,
            x.Code,
            x.Name,
            (int)x.ProviderType,
            x.ApiUrl,
            x.ApiUsername,
            x.ApiToken
        ));
    }
}
