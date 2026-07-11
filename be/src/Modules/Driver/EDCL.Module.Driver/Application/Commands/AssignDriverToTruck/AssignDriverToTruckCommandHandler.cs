using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.AssignDriverToTruck;

internal sealed class AssignDriverToTruckCommandHandler(DriverDbContext dbContext, IDriverPort driverPort) 
    : IRequestHandler<AssignDriverToTruckCommand, Result>
{
    public async Task<Result> Handle(AssignDriverToTruckCommand request, CancellationToken cancellationToken)
    {
        var truck = await dbContext.Trucks
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == request.TruckId, cancellationToken);
            
        if (truck is null) return Result.Failure(new Error("Truck.NotFound", "Truck not found"));
        
        // Verify driver exists and is active using Shared Port
        var driverInfo = await driverPort.GetActiveDriverByIdAsync(request.DriverId, cancellationToken);
        if (driverInfo is null) return Result.Failure(new Error("Driver.NotFoundOrInactive", "Driver not found or is inactive"));

        if (truck.TransporterId != driverInfo.TransporterId)
            return Result.Failure(new Error("Truck.TransporterMismatch", "Driver does not belong to the same Transporter as the Truck"));

        var isDriverAlreadyAssigned = await dbContext.TruckDriverAssignments
            .AnyAsync(a => a.DriverId == request.DriverId && a.IsActive, cancellationToken);
            
        if (isDriverAlreadyAssigned)
            return Result.Failure(new Error("Driver.AlreadyAssigned", "Driver is already assigned to an active truck and must be unassigned first"));
        
        var result = truck.AssignDriver(request.DriverId);
        if (result.IsFailure) return result;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}
