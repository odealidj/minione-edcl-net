using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.AssignDriverToTruck;

public sealed record AssignDriverToTruckCommand(long TruckId, long DriverId) : IRequest<Result>;
