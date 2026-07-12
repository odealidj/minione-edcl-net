using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.UpdateRoute;

public sealed record UpdateRouteCommand(long Id, string RouteCode, string CycleCode) : IRequest<Result>;
