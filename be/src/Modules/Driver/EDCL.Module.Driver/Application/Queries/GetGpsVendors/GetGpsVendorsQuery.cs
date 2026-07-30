using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetGpsVendors;

public sealed record GetGpsVendorsQuery(string? Search = null, int PageNumber = 1, int PageSize = 10)
    : IRequest<Result<GetGpsVendorsResponse>>;

public sealed record GetGpsVendorsResponse(
    System.Collections.Generic.IReadOnlyList<GpsVendorDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
