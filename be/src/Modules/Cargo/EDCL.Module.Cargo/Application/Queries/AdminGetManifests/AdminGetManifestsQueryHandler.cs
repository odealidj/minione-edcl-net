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
            query = query.Where(x => x.ManifestNo.Contains(request.Search) || x.SupplierName.Contains(request.Search));
        }

        if (!string.IsNullOrWhiteSpace(request.SupplierCode))
        {
            query = query.Where(x => x.SupplierCode == request.SupplierCode);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (request.IsAssignedToRoute.HasValue)
        {
            query = query.Where(x => x.IsAssignedToRoute == request.IsAssignedToRoute.Value);
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AdminManifestDto(
                x.Id, 
                x.ManifestNo, 
                x.SupplierCode,
                x.SupplierName, 
                x.Kanbans.Count(), 
                x.Parts.Count(),
                x.Status
            ))
            .ToListAsync(cancellationToken);

        var dtos = items.ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<AdminGetManifestsResponse>.Success(new AdminGetManifestsResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
