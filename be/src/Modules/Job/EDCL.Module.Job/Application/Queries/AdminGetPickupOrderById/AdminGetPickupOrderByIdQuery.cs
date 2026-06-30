using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Queries.AdminGetPickupOrderById;

public sealed record AdminGetPickupOrderByIdQuery(long Id) : IRequest<Result<AdminPickupOrderDto>>;
