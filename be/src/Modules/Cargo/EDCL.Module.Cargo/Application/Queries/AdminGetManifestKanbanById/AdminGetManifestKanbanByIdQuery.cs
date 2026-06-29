using EDCL.Shared.Kernel.Common;
using MediatR;
using EDCL.Module.Cargo.Application.DTOs;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestKanbanById;

public sealed record AdminGetManifestKanbanByIdQuery(long Id) : IRequest<Result<AdminManifestKanbanDto>>;
