using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.UpdateDriver;

public sealed record UpdateDriverCommand(long Id, string Name, string Nik, string PhoneNumber, long? LogisticPartnerId) : IRequest<Result>;
