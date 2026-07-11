using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.CreateTransporter;

public sealed record CreateTransporterCommand(string Name) : IRequest<Result<long>>;
