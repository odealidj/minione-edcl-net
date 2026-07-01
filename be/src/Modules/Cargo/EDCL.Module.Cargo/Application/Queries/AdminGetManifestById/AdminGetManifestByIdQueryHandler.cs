using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestById;

internal sealed class AdminGetManifestByIdQueryHandler(CargoDbContext dbContext) 
    : IRequestHandler<AdminGetManifestByIdQuery, Result<AdminManifestDto>>
{
    public async Task<Result<AdminManifestDto>> Handle(AdminGetManifestByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.Manifests
            .AsNoTracking()
            .Include(m => m.Kanbans)
            .Include(m => m.Parts)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (x is null) return Result<AdminManifestDto>.Failure(Error.NotFound("Manifest.NotFound", "Manifest not found."));

        return Result<AdminManifestDto>.Success(new AdminManifestDto(x.Id, x.ManifestNo, x.SupplierCode, x.SupplierName, x.Kanbans.Count, x.Parts.Count, x.Status));
    }
}
