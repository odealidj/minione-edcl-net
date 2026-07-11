using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;
using EDCL.Shared.Kernel.Domain;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetTransporters;

public sealed record GetTransportersQuery(string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<GetTransportersResponse>>;

public sealed record GetTransportersResponse(
    System.Collections.Generic.IReadOnlyList<TransporterDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
