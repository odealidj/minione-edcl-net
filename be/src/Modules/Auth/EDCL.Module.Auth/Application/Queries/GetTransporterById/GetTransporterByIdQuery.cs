using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Auth.Application.DTOs;

namespace EDCL.Module.Auth.Application.Queries.GetTransporterById;

public sealed record GetTransporterByIdQuery(long Id) : IRequest<Result<TransporterDto>>;
