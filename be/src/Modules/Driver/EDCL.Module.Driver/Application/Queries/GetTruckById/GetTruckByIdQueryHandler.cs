using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Queries.GetTruckById;

internal sealed class GetTruckByIdQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetTruckByIdQuery, Result<TruckDto>>
{
    public async Task<Result<TruckDto>> Handle(GetTruckByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.Trucks
            .Include(t => t.LogisticPartner)
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (x is null) return Result<TruckDto>.Failure(Error.NotFound("Truck.NotFound", "Truck not found."));

        return Result<TruckDto>.Success(new TruckDto(x.Id, x.PlateNumber, x.VehicleType, x.LogisticPartnerId, x.LogisticPartner?.Name, x.IsActive));
    }
}
