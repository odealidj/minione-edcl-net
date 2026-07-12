using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using EDCL.Module.Driver.Application.DTOs;

namespace EDCL.Module.Driver.Application.Queries.GetLogisticPartnerById;

public sealed record GetLogisticPartnerByIdQuery(long Id) : IRequest<Result<LogisticPartnerDto>>;
