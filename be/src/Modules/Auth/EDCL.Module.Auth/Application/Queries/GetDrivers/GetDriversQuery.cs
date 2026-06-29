using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Domain;
using EDCL.Module.Auth.Application.DTOs;

namespace EDCL.Module.Auth.Application.Queries.GetDrivers;

public sealed record GetDriversQuery(string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<GetDriversResponse>>;

public sealed record GetDriversResponse(
    System.Collections.Generic.IReadOnlyList<DriverDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
