using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.CreateSupplier;

internal sealed class CreateSupplierCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<CreateSupplierCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var entity = EDCL.Module.Driver.Domain.Entities.Supplier.Create(request.SupplierCode, request.Name, request.Address, request.Latitude, request.Longitude, request.GeofenceRadiusMeters);
        dbContext.Suppliers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(entity.Id);
    }
}
