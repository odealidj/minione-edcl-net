using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.StartJob;

public sealed record StartJobCommand(long PickupOrderId, long DriverId) : IRequest<Result<bool>>;
