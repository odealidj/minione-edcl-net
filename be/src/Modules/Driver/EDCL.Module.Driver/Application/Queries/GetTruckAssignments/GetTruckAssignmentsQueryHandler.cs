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
using System;

namespace EDCL.Module.Driver.Application.Queries.GetTruckAssignments;

internal sealed class GetTruckAssignmentsQueryHandler(DriverDbContext dbContext, IDriverPort driverPort)
    : IRequestHandler<GetTruckAssignmentsQuery, Result<GetTruckAssignmentsResponse>>
{
    public async Task<Result<GetTruckAssignmentsResponse>> Handle(GetTruckAssignmentsQuery request, CancellationToken cancellationToken)
    {
        // 1. Build base query for active assignments
        var query = dbContext.TruckDriverAssignments
            .Include(a => a.Truck)
            .ThenInclude(t => t!.LogisticPartner)
            .Where(a => a.IsActive && !a.Truck!.IsDeleted)
            .AsNoTracking();

        // 2. Get total count first for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return Result<GetTruckAssignmentsResponse>.Success(
                new GetTruckAssignmentsResponse(new List<TruckDriverAssignmentDto>(), 0, request.PageNumber, request.PageSize, 0));
        }

        // 3. Apply pagination (sort by AssignedAt desc, then paginate)
        var activeAssignments = await query
            .OrderByDescending(a => a.AssignedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // 4. Extract unique driver IDs from the current page and fetch in bulk
        var driverIds = activeAssignments.Select(a => a.DriverId).Distinct().ToList();
        var drivers = await driverPort.GetDriversByIdsAsync(driverIds, cancellationToken);
        var driverMap = drivers.ToDictionary(d => d.Id, d => d);

        // 5. Apply search filter if provided (filter on the fetched page by driver name / plate)
        IEnumerable<TruckDriverAssignmentDto> dtos = activeAssignments.Select(a =>
        {
            var driver = driverMap.GetValueOrDefault(a.DriverId);
            return new TruckDriverAssignmentDto(
                TruckId: a.TruckId,
                PlateNumber: a.Truck!.PlateNumber,
                LogisticPartnerId: a.Truck.LogisticPartnerId,
                LogisticPartnerName: a.Truck.LogisticPartner?.Name,
                DriverId: a.DriverId,
                DriverName: driver?.Name ?? "Unknown",
                DriverNik: driver?.Nik ?? "-",
                AssignedAt: a.AssignedAt
            );
        });

        // Apply search on mapped DTOs (driver name / plate number)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            dtos = dtos.Where(d =>
                d.PlateNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.DriverName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                d.DriverNik.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (d.LogisticPartnerName != null && d.LogisticPartnerName.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var totalPages = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / request.PageSize) : 0;
        var result = dtos.ToList();

        return Result<GetTruckAssignmentsResponse>.Success(
            new GetTruckAssignmentsResponse(result, totalCount, request.PageNumber, request.PageSize, totalPages));
    }
}
