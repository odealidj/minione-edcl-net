using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.DeleteLogisticPartner;

public sealed record DeleteLogisticPartnerCommand(long Id) : IRequest<Result>;
