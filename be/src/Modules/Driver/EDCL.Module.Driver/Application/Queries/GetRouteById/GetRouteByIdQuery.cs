using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Application.DTOs;
using MediatR;

namespace EDCL.Module.Driver.Application.Queries.GetRouteById;

public sealed record GetRouteByIdQuery(long Id) : IRequest<Result<RouteDto>>;
