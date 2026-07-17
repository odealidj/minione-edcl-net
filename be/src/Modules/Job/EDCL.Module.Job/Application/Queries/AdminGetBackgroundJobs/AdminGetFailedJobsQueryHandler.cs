using EDCL.Shared.Kernel.Common;
using Hangfire;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Queries.AdminGetBackgroundJobs;

internal sealed class AdminGetFailedJobsQueryHandler 
    : IRequestHandler<AdminGetFailedJobsQuery, Result<List<BackgroundJobDto>>>
{
    public Task<Result<List<BackgroundJobDto>>> Handle(AdminGetFailedJobsQuery request, CancellationToken cancellationToken)
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var failedJobs = monitoringApi.FailedJobs(request.From, request.Count);

        var dtos = failedJobs.Select(j => new BackgroundJobDto(
            JobId: j.Key,
            State: "Failed",
            MethodName: j.Value.Job?.Method?.Name ?? "Unknown",
            EnqueueAt: null,
            FailedAt: j.Value.FailedAt,
            ExceptionMessage: j.Value.ExceptionMessage,
            ExceptionDetails: j.Value.ExceptionDetails
        )).ToList();

        return Task.FromResult(Result<List<BackgroundJobDto>>.Success(dtos));
    }
}
