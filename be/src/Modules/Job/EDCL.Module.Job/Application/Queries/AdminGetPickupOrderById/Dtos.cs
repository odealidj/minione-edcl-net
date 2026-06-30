namespace EDCL.Module.Job.Application.Queries.AdminGetPickupOrderById;

public sealed record AdminPickupOrderKanbanDto(long Id, string KanbanCode, string Status, DateTime ScannedAt);

public sealed record AdminPickupOrderManifestDto(long Id, string ManifestNo, string Status, int TotalKanban, int ScannedKanban, string OrderType, int TotalSkid, string DockCode, List<AdminPickupOrderKanbanDto> Kanbans);

public sealed record AdminPickupOrderDetailDto(long Id, long SupplierId, int Sequence, string Status, DateTime? ArrivedAt, DateTime? PickedUpAt, List<AdminPickupOrderManifestDto> Manifests);

public sealed record AdminPickupOrderDto(long Id, long? DriverId, long? TruckId, string PoNo, DateTime PickupDate, string RouteCode, string CycleCode, TimeSpan EstimatedDepartureTime, string Status, DateTime? StartedAt, DateTime? CompletedAt, List<AdminPickupOrderDetailDto> Details);
