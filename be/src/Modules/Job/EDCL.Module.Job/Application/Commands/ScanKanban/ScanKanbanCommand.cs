using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.ScanKanban;

public sealed record ScanKanbanCommand(long StopId, long ManifestId, string KanbanCode, long DriverId) : IRequest<Result<ScanKanbanResponse>>;

public sealed record ScanKanbanResponse(
    long ManifestId,
    string Status,
    int Scanned,
    int Total);
