using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetTruckById;

public sealed record GetTruckByIdQuery(long Id) : IRequest<Result<TruckDto>>;
