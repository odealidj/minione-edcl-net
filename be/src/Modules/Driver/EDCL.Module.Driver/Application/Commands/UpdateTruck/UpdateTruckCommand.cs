using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.UpdateTruck;

public sealed record UpdateTruckCommand(long Id, string PlateNumber, string? VehicleType, long LogisticPartnerId, bool IsSimulated = false, string? GpsVehicleId = null) : IRequest<Result>;
