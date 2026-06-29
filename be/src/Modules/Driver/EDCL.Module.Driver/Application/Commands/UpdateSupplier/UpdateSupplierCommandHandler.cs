using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.UpdateSupplier;

internal sealed class UpdateSupplierCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<UpdateSupplierCommand, Result>
{
    public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Suppliers
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Supplier.NotFound", "Supplier not found."));

        typeof(EDCL.Module.Driver.Domain.Entities.Supplier).GetProperty("SupplierCode")!.SetValue(entity, request.SupplierCode);
        typeof(EDCL.Module.Driver.Domain.Entities.Supplier).GetProperty("Name")!.SetValue(entity, request.Name);
        typeof(EDCL.Module.Driver.Domain.Entities.Supplier).GetProperty("Address")!.SetValue(entity, request.Address);
        entity.UpdateLocation(request.Latitude ?? 0, request.Longitude ?? 0, request.GeofenceRadiusMeters);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
