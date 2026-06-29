using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Auth.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EDCL.Module.Auth.Application.Queries.GetTransporters;

internal sealed class GetTransportersQueryHandler(AuthDbContext dbContext) 
    : IRequestHandler<GetTransportersQuery, Result<GetTransportersResponse>>
{
    public async Task<Result<GetTransportersResponse>> Handle(GetTransportersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Transporters.Where(x => !x.IsDeleted);
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new TransporterDto(x.Id, x.Name)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<GetTransportersResponse>.Success(new GetTransportersResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
