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

        if (!string.IsNullOrWhiteSpace(request.PoNo))
        {
            var poNo = request.PoNo.Trim().ToLower();
            query = query.Where(x => x.PoNo.ToLower().Contains(poNo));
        }

        if (!string.IsNullOrWhiteSpace(request.ManifestNo))
        {
            var manifestNo = request.ManifestNo.Trim().ToLower();
            query = query.Where(x => x.Details.Any(d => d.Manifests.Any(m => m.ManifestNo.ToLower().Contains(manifestNo))));
        }

        if (request.PickupDate.HasValue)
        {
            var date = request.PickupDate.Value.Date;
            query = query.Where(x => x.PickupDate.Date == date);
        }

        if (!string.IsNullOrWhiteSpace(request.RouteCode))
        {
            var routeCode = request.RouteCode.Trim().ToLower();
            query = query.Where(x => x.RouteCode.ToLower().Contains(routeCode) || x.CycleCode.ToLower().Contains(routeCode));
        }

        if (request.DriverId.HasValue)
        {
            query = query.Where(x => x.DriverId == request.DriverId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statuses = request.Status.Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            query = query.Where(x => statuses.Contains(x.Status));
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
