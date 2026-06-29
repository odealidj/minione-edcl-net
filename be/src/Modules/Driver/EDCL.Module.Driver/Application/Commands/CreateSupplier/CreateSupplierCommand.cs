using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Domain;
using MediatR;

namespace EDCL.Module.Driver.Application.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(string SupplierCode, string Name, string? Address, double? Latitude, double? Longitude, int? GeofenceRadiusMeters) : IRequest<Result<long>>;
