using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.UnassignDriverFromTruck;

internal sealed class UnassignDriverFromTruckCommandHandler(DriverDbContext dbContext)
    : IRequestHandler<UnassignDriverFromTruckCommand, Result>
{
    public async Task<Result> Handle(UnassignDriverFromTruckCommand request, CancellationToken cancellationToken)
    {
        var truck = await dbContext.Trucks
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == request.TruckId, cancellationToken);

        if (truck is null) return Result.Failure(new Error("Truck.NotFound", "Truck not found"));

        var result = truck.UnassignDriver(request.DriverId);
        if (result.IsFailure) return result;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
