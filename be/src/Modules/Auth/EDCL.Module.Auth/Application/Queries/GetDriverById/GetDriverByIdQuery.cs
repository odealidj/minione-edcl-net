using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Auth.Application.DTOs;

namespace EDCL.Module.Auth.Application.Queries.GetDriverById;

public sealed record GetDriverByIdQuery(long Id) : IRequest<Result<DriverDto>>;
