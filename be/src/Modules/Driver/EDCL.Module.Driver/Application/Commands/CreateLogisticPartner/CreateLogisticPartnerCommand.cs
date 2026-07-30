using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.CreateLogisticPartner;

public sealed record CreateLogisticPartnerCommand(string Code, string Name, System.Collections.Generic.List<long> GpsVendorIds) : IRequest<Result<long>>;
