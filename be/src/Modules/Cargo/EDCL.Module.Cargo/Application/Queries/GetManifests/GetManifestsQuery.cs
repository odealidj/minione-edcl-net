namespace EDCL.Module.Cargo.Application.Queries.GetManifests;

using EDCL.Shared.Kernel.Common;
using MediatR;

public sealed record GetManifestsQuery(long StopId) : IRequest<Result<ManifestsResponse>>;

public sealed record ManifestsResponse(
    long StopId,
    int TotalManifests,
    int ScannedManifests,
    IEnumerable<ManifestDto> Manifests);

public sealed record ManifestDto(
    long ManifestId,
    string ManifestNo,
    string SupplierCode,
    string SupplierName,
    int Sequence,
    string Cycle,
    string OrderType,
    string IngestionStatus,
    string JobStatus);
