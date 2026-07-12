using EDCL.Shared.Kernel.Common;
using MediatR;
using System.Collections.Generic;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetAvailableTrucks;

public sealed record GetAvailableTrucksQuery(long LogisticPartnerId) 
    : IRequest<Result<IReadOnlyList<TruckDto>>>;
