using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.DeleteTransporter;

public sealed record DeleteTransporterCommand(long Id) : IRequest<Result>;
