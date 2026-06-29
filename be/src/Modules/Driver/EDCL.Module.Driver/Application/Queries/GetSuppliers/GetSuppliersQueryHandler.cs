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

namespace EDCL.Module.Driver.Application.Queries.GetSuppliers;

internal sealed class GetSuppliersQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetSuppliersQuery, Result<GetSuppliersResponse>>
{
    public async Task<Result<GetSuppliersResponse>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Suppliers.Where(x => !x.IsDeleted);
        
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(x => x.SupplierCode.ToLower().Contains(searchTerm) || x.Name.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new SupplierDto(x.Id, x.SupplierCode, x.Name, x.Address, x.Latitude, x.Longitude, x.GeofenceRadiusMeters, x.IsActive)).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<GetSuppliersResponse>.Success(new GetSuppliersResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
