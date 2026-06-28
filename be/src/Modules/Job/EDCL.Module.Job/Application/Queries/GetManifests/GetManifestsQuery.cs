using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.GetManifests;

public sealed record GetManifestsQuery(long StopId, long DriverId) : IRequest<Result<ManifestListResponse>>;

public sealed record ManifestListResponse(
    string SupplierName,
    string SupplierCode,
    List<ManifestDto> Manifests
);

public sealed record ManifestDto(
    string ManifestNo,
    string Type,
    int TotalSkid,
    string DockCode,
    int NoOfKanban,
    int ScannedKanban,
    bool IsComplete
);
