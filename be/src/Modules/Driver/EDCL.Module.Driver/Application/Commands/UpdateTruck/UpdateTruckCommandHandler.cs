using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.UpdateTruck;

internal sealed class UpdateTruckCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<UpdateTruckCommand, Result>
{
    public async Task<Result> Handle(UpdateTruckCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Trucks
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Truck.NotFound", "Truck not found."));

        typeof(EDCL.Module.Driver.Domain.Entities.Truck).GetProperty("PlateNumber")!.SetValue(entity, request.PlateNumber);
        typeof(EDCL.Module.Driver.Domain.Entities.Truck).GetProperty("VehicleType")!.SetValue(entity, request.VehicleType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
