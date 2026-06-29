using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestParts;

internal sealed class AdminGetManifestPartsQueryHandler(CargoDbContext dbContext) 
    : IRequestHandler<AdminGetManifestPartsQuery, Result<AdminGetManifestPartsResponse>>
{
    public async Task<Result<AdminGetManifestPartsResponse>> Handle(AdminGetManifestPartsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ManifestParts.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.Manifest.ManifestNo.ToLower().Contains(searchTerm) || x.PartNo.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new AdminManifestPartDto(x.Id, x.PartNo, x.PartName)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<AdminGetManifestPartsResponse>.Success(new AdminGetManifestPartsResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
