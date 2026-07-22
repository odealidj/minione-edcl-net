using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Common;
using EDCL.Module.Cargo.Application.DTOs;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestParts;

public sealed record AdminGetManifestPartsQuery(long? ManifestId = null, string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<AdminGetManifestPartsResponse>>;

public sealed record AdminGetManifestPartsResponse(
    System.Collections.Generic.IReadOnlyList<AdminManifestPartDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
