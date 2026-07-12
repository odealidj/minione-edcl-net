using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EDCL.Module.Driver.Application.Queries.GetTrucks;

internal sealed class GetTrucksQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetTrucksQuery, Result<GetTrucksResponse>>
{
    public async Task<Result<GetTrucksResponse>> Handle(GetTrucksQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Trucks.Where(x => !x.IsDeleted);
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.PlateNumber.ToLower().Contains(searchTerm));
        }

        query = query
            .Include(t => t.LogisticPartner)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new TruckDto(x.Id, x.PlateNumber, x.VehicleType, x.LogisticPartnerId, x.LogisticPartner?.Name, x.IsActive)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<GetTrucksResponse>.Success(new GetTrucksResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
