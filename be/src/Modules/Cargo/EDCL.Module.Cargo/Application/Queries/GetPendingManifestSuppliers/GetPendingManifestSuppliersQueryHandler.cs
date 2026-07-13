using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EDCL.Module.Cargo.Application.Queries.GetPendingManifestSuppliers;

internal sealed class GetPendingManifestSuppliersQueryHandler(CargoDbContext dbContext)
    : IRequestHandler<GetPendingManifestSuppliersQuery, Result<IReadOnlyList<PendingManifestSupplierDto>>>
{
    public async Task<Result<IReadOnlyList<PendingManifestSupplierDto>>> Handle(GetPendingManifestSuppliersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await dbContext.Manifests
            .AsNoTracking()
            .Where(x => x.Status == "Pending" && !x.IsAssignedToRoute)
            .Select(x => new PendingManifestSupplierDto(x.SupplierCode, x.SupplierName))
            .Distinct()
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PendingManifestSupplierDto>>.Success(suppliers);
    }
}
