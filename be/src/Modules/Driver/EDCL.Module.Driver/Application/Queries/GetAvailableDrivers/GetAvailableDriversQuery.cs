using EDCL.Shared.Kernel.Common;
using MediatR;
using System.Collections.Generic;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetAvailableDrivers;

public sealed record GetAvailableDriversQuery(long TransporterId) 
    : IRequest<Result<IReadOnlyList<AvailableDriverDto>>>;
