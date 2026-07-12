using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.CreateTruck;

internal sealed class CreateTruckCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<CreateTruckCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateTruckCommand request, CancellationToken cancellationToken)
    {
        var entity = EDCL.Module.Driver.Domain.Entities.Truck.Create(request.PlateNumber, request.LogisticPartnerId, request.VehicleType);
        dbContext.Trucks.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(entity.Id);
    }
}
