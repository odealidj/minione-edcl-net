using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestProblems;

internal sealed class AdminGetManifestProblemsQueryHandler(CargoDbContext dbContext) 
    : IRequestHandler<AdminGetManifestProblemsQuery, Result<AdminGetManifestProblemsResponse>>
{
    public async Task<Result<AdminGetManifestProblemsResponse>> Handle(AdminGetManifestProblemsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ManifestProblems.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x => (x.ManifestNo != null && x.ManifestNo.Contains(request.Search)) 
                                  || x.Description.Contains(request.Search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(x => x.Status == request.Status);
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AdminManifestProblemDto(
                x.Id, 
                x.OperationType,
                x.Payload,
                x.Description,
                x.Status,
                x.OccurredAt,
                x.ManifestNo,
                x.DeliveryNo,
                x.PickupOrderId,
                x.ResolvedBy,
                x.ResolvedAt,
                x.ResolutionReason
            ))
            .ToListAsync(cancellationToken);

        var dtos = items.ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<AdminGetManifestProblemsResponse>.Success(new AdminGetManifestProblemsResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
