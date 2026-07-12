using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.DeleteRoute;

public sealed record DeleteRouteCommand(long Id) : IRequest<Result>;
