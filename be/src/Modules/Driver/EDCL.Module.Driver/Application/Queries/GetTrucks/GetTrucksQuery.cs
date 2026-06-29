using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Domain;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetTrucks;

public sealed record GetTrucksQuery(string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<GetTrucksResponse>>;

public sealed record GetTrucksResponse(
    System.Collections.Generic.IReadOnlyList<TruckDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
