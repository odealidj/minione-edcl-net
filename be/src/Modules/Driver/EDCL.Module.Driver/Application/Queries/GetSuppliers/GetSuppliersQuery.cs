using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Domain;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetSuppliers;

public sealed record GetSuppliersQuery(string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<GetSuppliersResponse>>;

public sealed record GetSuppliersResponse(
    System.Collections.Generic.IReadOnlyList<SupplierDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
