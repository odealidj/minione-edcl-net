namespace EDCL.Module.Job.Application.Queries.AdminGetDashboardSummary;

public sealed record AdminGetDashboardSummaryResponse(
    DashboardKpiDto Kpis,
    List<RouteDistributionDto> RouteDistributions,
    List<LateDepartureAlertDto> LateDepartures
);

public sealed record DashboardKpiDto(
    int TotalOrders,
    int PendingOrders,
    int OnProgressOrders,
    int CompletedOrders,
    int TotalKanban
);

public sealed record RouteDistributionDto(
    string RouteCode,
    int OrderCount
);

public sealed record LateDepartureAlertDto(
    string PoNo,
    string RouteCode,
    TimeSpan EstimatedDepartureTime,
    string Status
);
