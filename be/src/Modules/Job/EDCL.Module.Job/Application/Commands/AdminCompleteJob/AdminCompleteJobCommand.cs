using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.AdminCompleteJob;

public sealed record AdminCompleteJobCommand(long PickupOrderId, string Reason) : IRequest<Result<bool>>;
