using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Job.Application.Commands.EndJob;

public sealed record EndJobCommand(long PickupOrderId, long DriverId, double Latitude, double Longitude) : IRequest<Result<bool>>;
