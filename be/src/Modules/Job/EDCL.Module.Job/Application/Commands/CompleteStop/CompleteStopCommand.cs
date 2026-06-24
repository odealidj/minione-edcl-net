using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.CompleteStop;

public sealed record CompleteStopCommand(long StopId, long DriverId) : IRequest<Result<bool>>;
