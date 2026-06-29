using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.DeleteTruck;

public sealed record DeleteTruckCommand(long Id) : IRequest<Result>;
