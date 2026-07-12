using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.CreateRoute;

public sealed record CreateRouteCommand(string RouteCode, string CycleCode) : IRequest<Result<long>>;
