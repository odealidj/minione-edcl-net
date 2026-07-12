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

namespace EDCL.Module.Auth.Application.Queries.GetDrivers;

internal sealed class GetDriversQueryHandler(AuthDbContext dbContext) 
    : IRequestHandler<GetDriversQuery, Result<GetDriversResponse>>
{
    public async Task<Result<GetDriversResponse>> Handle(GetDriversQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Drivers.Where(x => !x.IsDeleted);
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Nik.ToLower().Contains(searchTerm) || x.PhoneNumber.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new DriverDto(x.Id, x.Name, x.Nik, x.PhoneNumber, x.IsActive, x.LogisticPartnerId)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<GetDriversResponse>.Success(new GetDriversResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
