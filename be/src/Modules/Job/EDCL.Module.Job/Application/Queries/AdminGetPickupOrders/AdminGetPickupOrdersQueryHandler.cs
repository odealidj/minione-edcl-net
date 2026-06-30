using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Queries.AdminGetPickupOrders;

internal sealed class AdminGetPickupOrdersQueryHandler(JobDbContext dbContext) 
    : IRequestHandler<AdminGetPickupOrdersQuery, Result<AdminGetPickupOrdersResponse>>
{
    public async Task<Result<AdminGetPickupOrdersResponse>> Handle(AdminGetPickupOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.PickupOrders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x => 
                x.PoNo.ToLower().Contains(s) || 
                x.RouteCode.ToLower().Contains(s) || 
                x.CycleCode.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AdminPickupOrderListItemDto(
                x.Id, x.DriverId, x.TruckId, x.PoNo, x.PickupDate, x.RouteCode, x.CycleCode, x.EstimatedDepartureTime, x.Status, x.StartedAt, x.CompletedAt
            ))
            .ToListAsync(cancellationToken);

        var totalPages = (int)System.Math.Ceiling(totalCount / (double)request.PageSize);
        return Result<AdminGetPickupOrdersResponse>.Success(new AdminGetPickupOrdersResponse(items, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
