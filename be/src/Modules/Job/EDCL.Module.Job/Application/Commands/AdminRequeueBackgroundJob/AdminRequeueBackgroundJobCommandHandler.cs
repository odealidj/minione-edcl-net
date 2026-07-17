using EDCL.Shared.Kernel.Common;
using Hangfire;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Job.Application.Commands.AdminRequeueBackgroundJob;

internal sealed class AdminRequeueBackgroundJobCommandHandler 
    : IRequestHandler<AdminRequeueBackgroundJobCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(AdminRequeueBackgroundJobCommand request, CancellationToken cancellationToken)
    {
        var requeued = BackgroundJob.Requeue(request.JobId);
        return Task.FromResult(Result<bool>.Success(requeued));
    }
}
