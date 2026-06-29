using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.DeleteTruck;

internal sealed class DeleteTruckCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<DeleteTruckCommand, Result>
{
    public async Task<Result> Handle(DeleteTruckCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Trucks
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Truck.NotFound", "Truck not found."));

        entity.SoftDelete("SYSTEM");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
