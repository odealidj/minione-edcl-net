using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Cargo.Application.Queries.GetManifestDetail;

public sealed record GetManifestDetailQuery(string ManifestNo) : IRequest<Result<ManifestDetailDto>>;

public sealed record ManifestDetailDto(
    string RouteCycle,
    string DeliveryNo,
    string ManifestNo,
    int TotalKanban,
    string? OrderNo,
    string? DockCode,
    string? PLaneNo,
    List<ManifestPartDto> PartList,
    List<ManifestKanbanDto> KanbanList
);

public sealed record ManifestPartDto(
    int No,
    string PartNo,
    string UniqNo,
    int PcsKbn,
    string BoxType,
    string NoOfKbn
);

public sealed record ManifestKanbanDto(
    string PartNo,
    string KanbanCd
);
