using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.AdminGetBackgroundJobs;

public sealed record AdminGetScheduledJobsQuery(int From = 0, int Count = 100) : IRequest<Result<List<BackgroundJobDto>>>;
