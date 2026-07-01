using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EDCL.Module.Job.Application.Queries.AdminGetPickupOrderById;
using System.Collections.Generic;

namespace EDCL.Module.Job.Application.Queries.AdminGetPickupOrderManifests;

internal sealed class AdminGetPickupOrderManifestsQueryHandler(JobDbContext dbContext) 
    : IRequestHandler<AdminGetPickupOrderManifestsQuery, Result<AdminGetPickupOrderManifestsResponse>>
{
    public async Task<Result<AdminGetPickupOrderManifestsResponse>> Handle(AdminGetPickupOrderManifestsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.PickupOrderManifests
            .AsNoTracking()
            .Include(m => m.Kanbans)
            .Where(m => m.PickupOrderDetailId == request.StopId);

        var totalCount = await query.CountAsync(cancellationToken);

        var manifests = await query
            .OrderBy(m => m.ManifestNo)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = manifests.Select(m => new AdminPickupOrderManifestDto(
            m.Id, m.ManifestNo, m.Status, m.TotalKanban, m.ScannedKanban, m.OrderType, m.TotalSkid, m.DockCode,
            m.Kanbans.Select(k => new AdminPickupOrderKanbanDto(
                k.Id, k.KanbanCode, k.Status, k.ScannedAt
            )).ToList()
        )).ToList();

        return Result<AdminGetPickupOrderManifestsResponse>.Success(
            new AdminGetPickupOrderManifestsResponse(dtos, totalCount, request.PageNumber, request.PageSize)
        );
    }
}
