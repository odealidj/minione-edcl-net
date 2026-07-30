using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetGpsVendorById;

public sealed record GetGpsVendorByIdQuery(long Id) : IRequest<Result<GpsVendorDto>>;
