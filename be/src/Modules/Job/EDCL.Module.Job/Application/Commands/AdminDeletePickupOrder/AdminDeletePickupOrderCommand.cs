using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.AdminDeletePickupOrder;

public sealed record AdminDeletePickupOrderCommand(long Id) : IRequest<Result<bool>>;
