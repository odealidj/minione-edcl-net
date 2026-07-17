using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.AdminRequeueBackgroundJob;

public sealed record AdminRequeueBackgroundJobCommand(string JobId) : IRequest<Result<bool>>;
