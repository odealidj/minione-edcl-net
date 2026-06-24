namespace EDCL.Module.Cargo.Application.Queries.GetManifestParts;

using EDCL.Shared.Kernel.Common;
using MediatR;

public sealed record GetManifestPartsQuery(long ManifestId) : IRequest<Result<ManifestPartsResponse>>;

public sealed record ManifestPartsResponse(
    long ManifestId,
    string ManifestNo,
    int TotalParts,
    int TotalScannedParts,
    IEnumerable<ManifestPartDto> Parts);

public sealed record ManifestPartDto(
    long PartId,
    string PartNo,
    string PartName,
    int Qty,
    string KanbanNo,
    string Status);
