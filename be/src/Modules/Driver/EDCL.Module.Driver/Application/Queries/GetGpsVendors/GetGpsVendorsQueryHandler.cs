using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Queries.GetGpsVendors;

internal sealed class GetGpsVendorsQueryHandler(DriverDbContext dbContext)
    : IRequestHandler<GetGpsVendorsQuery, Result<GetGpsVendorsResponse>>
{
    public async Task<Result<GetGpsVendorsResponse>> Handle(GetGpsVendorsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.GpsVendors.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(search) || x.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(x => new GpsVendorDto(
            x.Id,
            x.Code,
            x.Name,
            (int)x.ProviderType,
            x.ApiUrl,
            x.ApiUsername,
            x.ApiToken,
            x.ConnectionStatus,
            x.LastCheckedAt
        )).ToList();

        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;

        return Result<GetGpsVendorsResponse>.Success(
            new GetGpsVendorsResponse(dtos, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
