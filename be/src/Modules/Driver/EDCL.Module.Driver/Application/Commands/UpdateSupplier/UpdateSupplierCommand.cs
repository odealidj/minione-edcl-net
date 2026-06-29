using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(long Id, string SupplierCode, string Name, string? Address, double? Latitude, double? Longitude, int? GeofenceRadiusMeters) : IRequest<Result>;
