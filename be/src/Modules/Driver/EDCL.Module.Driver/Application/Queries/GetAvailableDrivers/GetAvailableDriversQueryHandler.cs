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

namespace EDCL.Module.Driver.Application.Queries.GetAvailableDrivers;

internal sealed class GetAvailableDriversQueryHandler(DriverDbContext dbContext, IDriverPort driverPort)
    : IRequestHandler<GetAvailableDriversQuery, Result<IReadOnlyList<AvailableDriverDto>>>
{
    public async Task<Result<IReadOnlyList<AvailableDriverDto>>> Handle(GetAvailableDriversQuery request, CancellationToken cancellationToken)
    {
        var allDriversForTransporter = await driverPort.GetActiveDriversByTransporterIdAsync(request.TransporterId, cancellationToken);
        
        if (allDriversForTransporter.Count == 0)
        {
            return Result<IReadOnlyList<AvailableDriverDto>>.Success(new List<AvailableDriverDto>());
        }

        var activeAssignedDriverIds = await dbContext.TruckDriverAssignments
            .Where(a => a.IsActive)
            .Select(a => a.DriverId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var availableDrivers = allDriversForTransporter
            .Where(d => !activeAssignedDriverIds.Contains(d.Id))
            .OrderBy(d => d.Name)
            .Select(d => new AvailableDriverDto(
                Id: d.Id,
                Name: d.Name,
                Nik: d.Nik,
                PhoneNumber: d.PhoneNumber
            ))
            .ToList();

        return Result<IReadOnlyList<AvailableDriverDto>>.Success(availableDrivers);
    }
}
