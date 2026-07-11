using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.UnassignDriverFromTruck;

public sealed record UnassignDriverFromTruckCommand(long TruckId, long DriverId) : IRequest<Result>;
