using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace EDCL.Module.Job.Application.Queries.AdminGetDashboardSummary;

internal sealed class AdminGetDashboardSummaryQueryHandler(JobDbContext dbContext) 
    : IRequestHandler<AdminGetDashboardSummaryQuery, Result<AdminGetDashboardSummaryResponse>>
{
    public async Task<Result<AdminGetDashboardSummaryResponse>> Handle(AdminGetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var targetDate = request.Date?.Date ?? DateTime.UtcNow.Date;
        var currentTime = DateTime.UtcNow.TimeOfDay;

        var query = dbContext.PickupOrders
            .AsNoTracking()
            .Where(x => x.PickupDate == targetDate);

        var orders = await query.ToListAsync(cancellationToken);

        // KPIs
        var totalOrders = orders.Count;
        var pendingOrders = orders.Count(x => x.Status == PickupOrderStatus.Pending);
        var onProgressOrders = orders.Count(x => x.Status == PickupOrderStatus.OnProgress);
        var completedOrders = orders.Count(x => x.Status == PickupOrderStatus.Completed);
        
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var scheduledJobsCount = monitoringApi.ScheduledCount();
        var failedJobsCount = monitoringApi.FailedCount();
        
        var kpis = new DashboardKpiDto(totalOrders, pendingOrders, onProgressOrders, completedOrders, 0, scheduledJobsCount, failedJobsCount);

        // Route Distributions
        var routeDistributions = orders
            .GroupBy(x => x.RouteCode)
            .Select(g => new RouteDistributionDto(g.Key, g.Count()))
            .OrderByDescending(x => x.OrderCount)
            .ToList();

        // Late Departures
        // Pending orders that missed their EST
        var lateDepartures = orders
            .Where(x => x.Status == PickupOrderStatus.Pending && currentTime > x.EstimatedDepartureTime)
            .OrderByDescending(x => (currentTime - x.EstimatedDepartureTime).TotalMinutes)
            .Select(x => new LateDepartureAlertDto(x.PoNo, x.RouteCode, x.EstimatedDepartureTime, x.Status))
            .ToList();

        return new AdminGetDashboardSummaryResponse(kpis, routeDistributions, lateDepartures);
    }
}
