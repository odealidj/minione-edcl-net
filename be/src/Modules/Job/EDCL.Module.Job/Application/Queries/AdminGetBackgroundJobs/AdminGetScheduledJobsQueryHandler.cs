using EDCL.Shared.Kernel.Common;
using Hangfire;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Queries.AdminGetBackgroundJobs;

internal sealed class AdminGetScheduledJobsQueryHandler 
    : IRequestHandler<AdminGetScheduledJobsQuery, Result<List<BackgroundJobDto>>>
{
    public Task<Result<List<BackgroundJobDto>>> Handle(AdminGetScheduledJobsQuery request, CancellationToken cancellationToken)
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var scheduledJobs = monitoringApi.ScheduledJobs(request.From, request.Count);

        var dtos = scheduledJobs.Select(j => new BackgroundJobDto(
            JobId: j.Key,
            State: "Scheduled",
            MethodName: j.Value.Job?.Method?.Name ?? "Unknown",
            EnqueueAt: j.Value.EnqueueAt,
            FailedAt: null,
            ExceptionMessage: null,
            ExceptionDetails: null
        )).ToList();

        return Task.FromResult(Result<List<BackgroundJobDto>>.Success(dtos));
    }
}
