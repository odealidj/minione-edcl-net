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

namespace EDCL.Module.Driver.Application.Queries.GetLogisticPartners;

internal sealed class GetLogisticPartnersQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetLogisticPartnersQuery, Result<GetLogisticPartnersResponse>>
{
    public async Task<Result<GetLogisticPartnersResponse>> Handle(GetLogisticPartnersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.LogisticPartners
            .Include(x => x.GpsVendorMappings)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(search) || x.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new LogisticPartnerDto(
            x.Id,
            x.Code,
            x.Name,
            x.GpsVendorMappings.Select(m => m.GpsVendorId).ToList()
        )).ToList();
        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<GetLogisticPartnersResponse>.Success(new GetLogisticPartnersResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
