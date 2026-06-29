using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetSupplierById;

public sealed record GetSupplierByIdQuery(long Id) : IRequest<Result<SupplierDto>>;
