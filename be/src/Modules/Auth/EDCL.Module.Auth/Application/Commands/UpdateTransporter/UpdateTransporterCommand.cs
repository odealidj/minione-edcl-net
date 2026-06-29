using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.UpdateTransporter;

public sealed record UpdateTransporterCommand(long Id, string Name) : IRequest<Result>;
