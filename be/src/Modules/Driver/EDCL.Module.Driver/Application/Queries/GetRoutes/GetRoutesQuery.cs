using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Application.DTOs;
using MediatR;

namespace EDCL.Module.Driver.Application.Queries.GetRoutes;

public sealed record GetRoutesQuery(string? Search, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<GetRoutesResponse>>;

public sealed record GetRoutesResponse(
    IReadOnlyList<RouteDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);
