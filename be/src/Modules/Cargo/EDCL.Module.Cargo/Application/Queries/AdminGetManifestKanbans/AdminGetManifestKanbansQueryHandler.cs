using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestKanbans;

internal sealed class AdminGetManifestKanbansQueryHandler(CargoDbContext dbContext) 
    : IRequestHandler<AdminGetManifestKanbansQuery, Result<AdminGetManifestKanbansResponse>>
{
    public async Task<Result<AdminGetManifestKanbansResponse>> Handle(AdminGetManifestKanbansQuery request, CancellationToken cancellationToken)
    {
        var query = from k in dbContext.ManifestKanbans
                    join p in dbContext.ManifestParts 
                        on new { k.ManifestId, k.PartNo } equals new { p.ManifestId, p.PartNo } into kanbanParts
                    from p in kanbanParts.DefaultIfEmpty()
                    select new { Kanban = k, PartName = p != null ? p.PartName : "-" };
        
        if (request.ManifestId.HasValue)
        {
            query = query.Where(x => x.Kanban.ManifestId == request.ManifestId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.Kanban!.Manifest!.ManifestNo.ToLower().Contains(searchTerm) || x.Kanban!.PartNo.ToLower().Contains(searchTerm) || x.Kanban!.KanbanCd.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Kanban.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new AdminManifestKanbanDto(x.Kanban.Id, x.Kanban.PartNo, x.PartName, x.Kanban.KanbanCd)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<AdminGetManifestKanbansResponse>.Success(new AdminGetManifestKanbansResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
