using EDCL.Shared.Kernel.Common;
using MediatR;
using EDCL.Module.Cargo.Application.DTOs;

namespace EDCL.Module.Cargo.Application.Queries.AdminGetManifestPartById;

public sealed record AdminGetManifestPartByIdQuery(long Id) : IRequest<Result<AdminManifestPartDto>>;
