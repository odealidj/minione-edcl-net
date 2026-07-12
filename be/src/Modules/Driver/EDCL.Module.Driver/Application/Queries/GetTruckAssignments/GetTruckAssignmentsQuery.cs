using EDCL.Shared.Kernel.Common;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;
using System.Collections.Generic;

namespace EDCL.Module.Driver.Application.Queries.GetTruckAssignments;

public sealed record GetTruckAssignmentsQuery(
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<Result<GetTruckAssignmentsResponse>>;

public sealed record GetTruckAssignmentsResponse(
    System.Collections.Generic.IReadOnlyList<TruckDriverAssignmentDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
