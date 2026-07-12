using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.UpdateLogisticPartner;

public sealed record UpdateLogisticPartnerCommand(long Id, string Code, string Name) : IRequest<Result>;
