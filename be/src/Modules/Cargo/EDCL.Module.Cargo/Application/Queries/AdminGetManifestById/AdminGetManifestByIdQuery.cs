using EDCL.Shared.Kernel.Common;
using MediatR;
using EDCL.Module.Cargo.Application.DTOs;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestById;

public sealed record AdminGetManifestByIdQuery(long Id) : IRequest<Result<AdminManifestDto>>;
