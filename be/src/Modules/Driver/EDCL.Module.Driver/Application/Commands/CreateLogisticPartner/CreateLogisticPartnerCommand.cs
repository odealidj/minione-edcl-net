using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.CreateLogisticPartner;

public sealed record CreateLogisticPartnerCommand(string Code, string Name) : IRequest<Result<long>>;
