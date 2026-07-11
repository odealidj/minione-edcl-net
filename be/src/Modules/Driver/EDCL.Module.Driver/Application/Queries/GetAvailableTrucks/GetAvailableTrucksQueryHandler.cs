using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EDCL.Module.Driver.Application.Queries.GetAvailableTrucks;

internal sealed class GetAvailableTrucksQueryHandler(DriverDbContext dbContext)
    : IRequestHandler<GetAvailableTrucksQuery, Result<IReadOnlyList<TruckDto>>>
{
    public async Task<Result<IReadOnlyList<TruckDto>>> Handle(GetAvailableTrucksQuery request, CancellationToken cancellationToken)
    {
        var activeAssignedTruckIds = await dbContext.TruckDriverAssignments
            .Where(a => a.IsActive)
            .Select(a => a.TruckId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var availableTrucks = await dbContext.Trucks
            .Include(t => t.Transporter)
            .Where(t => t.TransporterId == request.TransporterId && !t.IsDeleted && !activeAssignedTruckIds.Contains(t.Id))
            .AsNoTracking()
            .OrderBy(t => t.PlateNumber)
            .ToListAsync(cancellationToken);

        var dtos = availableTrucks.Select(t => new TruckDto(
            Id: t.Id,
            PlateNumber: t.PlateNumber,
            VehicleType: t.VehicleType,
            TransporterId: t.TransporterId,
            TransporterName: t.Transporter?.Name,
            IsActive: t.IsActive
        )).ToList();

        return Result<IReadOnlyList<TruckDto>>.Success(dtos);
    }
}
