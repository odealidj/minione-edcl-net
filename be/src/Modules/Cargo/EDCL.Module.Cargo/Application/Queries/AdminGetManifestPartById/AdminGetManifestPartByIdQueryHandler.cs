using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestPartById;

internal sealed class AdminGetManifestPartByIdQueryHandler(CargoDbContext dbContext) 
    : IRequestHandler<AdminGetManifestPartByIdQuery, Result<AdminManifestPartDto>>
{
    public async Task<Result<AdminManifestPartDto>> Handle(AdminGetManifestPartByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.ManifestParts
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == request.Id, cancellationToken);
            
        if (x is null) return Result<AdminManifestPartDto>.Failure(Error.NotFound("ManifestPart.NotFound", "ManifestPart not found."));

        return Result<AdminManifestPartDto>.Success(new AdminManifestPartDto(x.Id, x.PartNo, x.PartName));
    }
}
