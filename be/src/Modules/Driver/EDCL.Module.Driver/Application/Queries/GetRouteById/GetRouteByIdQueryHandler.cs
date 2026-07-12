using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Queries.GetRouteById;

internal sealed class GetRouteByIdQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetRouteByIdQuery, Result<RouteDto>>
{
    public async Task<Result<RouteDto>> Handle(GetRouteByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (x is null) return Result<RouteDto>.Failure(Error.NotFound("Route.NotFound", "Route not found."));

        return Result<RouteDto>.Success(new RouteDto(x.Id, x.RouteCode, x.CycleCode));
    }
}
