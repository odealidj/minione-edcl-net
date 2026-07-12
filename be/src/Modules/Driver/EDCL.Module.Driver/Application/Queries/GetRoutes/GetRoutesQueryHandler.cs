using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EDCL.Module.Driver.Application.Queries.GetRoutes;

internal sealed class GetRoutesQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetRoutesQuery, Result<GetRoutesResponse>>
{
    public async Task<Result<GetRoutesResponse>> Handle(GetRoutesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Routes.Where(x => !x.IsDeleted);
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.RouteCode.ToLower().Contains(searchTerm) || x.CycleCode.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new RouteDto(x.Id, x.RouteCode, x.CycleCode)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<GetRoutesResponse>.Success(new GetRoutesResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
