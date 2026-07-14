using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.AdminGetDashboardSummary;

public sealed record AdminGetDashboardSummaryQuery(DateTime? Date = null) : IRequest<Result<AdminGetDashboardSummaryResponse>>;
