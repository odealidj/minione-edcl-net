using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.CreateTruck;

public sealed record CreateTruckCommand(string PlateNumber, string? VehicleType) : IRequest<Result<long>>;
