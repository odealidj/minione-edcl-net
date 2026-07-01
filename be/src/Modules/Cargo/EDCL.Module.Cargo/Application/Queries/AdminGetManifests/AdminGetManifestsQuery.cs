using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Common;
using EDCL.Module.Cargo.Application.DTOs;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifests;

public sealed record AdminGetManifestsQuery(string? Search = null, string? SupplierCode = null, string? Status = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<AdminGetManifestsResponse>>;

public sealed record AdminGetManifestsResponse(
    System.Collections.Generic.IReadOnlyList<AdminManifestDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
