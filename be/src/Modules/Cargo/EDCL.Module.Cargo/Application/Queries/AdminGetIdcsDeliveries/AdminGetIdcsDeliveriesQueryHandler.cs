using EDCL.Module.Cargo.Application.DTOs;
using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetIdcsDeliveries;

public class AdminGetIdcsDeliveriesQueryHandler(IdcsDbContext dbContext) 
    : IRequestHandler<AdminGetIdcsDeliveriesQuery, ApiResponse<List<IdcsDeliveryDto>>>
{
    public async Task<ApiResponse<List<IdcsDeliveryDto>>> Handle(AdminGetIdcsDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.IdcsDeliveryStatuses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            query = query.Where(x => x.ManifestNo != null && x.ManifestNo.Contains(request.SearchQuery));
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(x => x.DeliveredAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new IdcsDeliveryDto
            {
                Id = x.Id,
                ManifestId = x.ManifestId,
                ManifestNo = x.ManifestNo,
                Status = x.Status,
                DeliveredAt = x.DeliveredAt,
                Remarks = x.Remarks
            })
            .ToListAsync(cancellationToken);

        var pagination = PaginationMeta.From(request.Page, request.PageSize, totalRecords);

        return ApiResponse<List<IdcsDeliveryDto>>.Paginated(data, pagination, "idcs-query");
    }
}
