namespace EDCL.Module.Cargo.Application.Queries.GetManifestKanbanDetails;

using EDCL.Shared.Kernel.Common;
using MediatR;

public sealed record GetManifestKanbanDetailsQuery(
    long ManifestId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PaginatedResult<ManifestKanbanDto>>>;

public sealed record ManifestKanbanDto(
    string KanbanCd,
    string PartNo,
    string PartName,
    int PartQty);

public sealed record PaginatedResult<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
