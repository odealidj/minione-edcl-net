using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Common;
using EDCL.Module.Cargo.Application.DTOs;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestProblems;

public sealed record AdminGetManifestProblemsQuery(string? Search = null, string? Status = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<AdminGetManifestProblemsResponse>>;

public sealed record AdminGetManifestProblemsResponse(
    System.Collections.Generic.IReadOnlyList<AdminManifestProblemDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
