using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.AssignJob;

public sealed record AssignJobCommand(long PickupOrderId, long DriverId, long? TruckId = null) : IRequest<Result<bool>>;
