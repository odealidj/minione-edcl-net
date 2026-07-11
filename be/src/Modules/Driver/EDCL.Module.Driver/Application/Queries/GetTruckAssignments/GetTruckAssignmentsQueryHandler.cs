using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EDCL.Module.Driver.Application.Queries.GetTruckAssignments;

internal sealed class GetTruckAssignmentsQueryHandler(DriverDbContext dbContext, IDriverPort driverPort)
    : IRequestHandler<GetTruckAssignmentsQuery, Result<IReadOnlyList<TruckDriverAssignmentDto>>>
{
    public async Task<Result<IReadOnlyList<TruckDriverAssignmentDto>>> Handle(GetTruckAssignmentsQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch active assignments with Truck and Transporter details
        var activeAssignments = await dbContext.TruckDriverAssignments
            .Include(a => a.Truck)
            .ThenInclude(t => t!.Transporter)
            .Where(a => a.IsActive && !a.Truck!.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (activeAssignments.Count == 0)
        {
            return Result<IReadOnlyList<TruckDriverAssignmentDto>>.Success(new List<TruckDriverAssignmentDto>());
        }

        // 2. Extract unique driver IDs
        var driverIds = activeAssignments.Select(a => a.DriverId).Distinct().ToList();

        // 3. Fetch driver details in bulk via port
        var drivers = await driverPort.GetDriversByIdsAsync(driverIds, cancellationToken);
        var driverMap = drivers.ToDictionary(d => d.Id, d => d);

        // 4. Map to DTOs
        var dtos = activeAssignments.Select(a => 
        {
            var driver = driverMap.GetValueOrDefault(a.DriverId);
            return new TruckDriverAssignmentDto(
                TruckId: a.TruckId,
                PlateNumber: a.Truck!.PlateNumber,
                TransporterId: a.Truck.TransporterId,
                TransporterName: a.Truck.Transporter?.Name,
                DriverId: a.DriverId,
                DriverName: driver?.Name ?? "Unknown",
                DriverNik: driver?.Nik ?? "-",
                AssignedAt: a.AssignedAt
            );
        }).OrderByDescending(d => d.AssignedAt).ToList();

        return Result<IReadOnlyList<TruckDriverAssignmentDto>>.Success(dtos);
    }
}
