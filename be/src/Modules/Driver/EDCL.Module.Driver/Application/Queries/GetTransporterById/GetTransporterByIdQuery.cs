using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetTransporterById;

public sealed record GetTransporterByIdQuery(long Id) : IRequest<Result<TransporterDto>>;
