using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.DeleteDriver;

public sealed record DeleteDriverCommand(long Id) : IRequest<Result>;
