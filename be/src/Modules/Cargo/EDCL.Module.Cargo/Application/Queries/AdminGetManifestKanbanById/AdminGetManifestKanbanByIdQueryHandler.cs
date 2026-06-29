using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestKanbanById;

internal sealed class AdminGetManifestKanbanByIdQueryHandler(CargoDbContext dbContext) 
    : IRequestHandler<AdminGetManifestKanbanByIdQuery, Result<AdminManifestKanbanDto>>
{
    public async Task<Result<AdminManifestKanbanDto>> Handle(AdminGetManifestKanbanByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.ManifestKanbans
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == request.Id, cancellationToken);
            
        if (x is null) return Result<AdminManifestKanbanDto>.Failure(Error.NotFound("ManifestKanban.NotFound", "ManifestKanban not found."));

        return Result<AdminManifestKanbanDto>.Success(new AdminManifestKanbanDto(x.Id, x.PartNo, "-", x.KanbanCd));
    }
}
