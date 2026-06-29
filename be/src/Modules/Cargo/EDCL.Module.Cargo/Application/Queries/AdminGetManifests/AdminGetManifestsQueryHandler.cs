using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifests;

internal sealed class AdminGetManifestsQueryHandler(CargoDbContext dbContext) 
    : IRequestHandler<AdminGetManifestsQuery, Result<AdminGetManifestsResponse>>
{
    public async Task<Result<AdminGetManifestsResponse>> Handle(AdminGetManifestsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Manifests.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.ManifestNo.ToLower().Contains(searchTerm) || x.SupplierName.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new AdminManifestDto(x.Id, x.ManifestNo, x.SupplierName)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<AdminGetManifestsResponse>.Success(new AdminGetManifestsResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
