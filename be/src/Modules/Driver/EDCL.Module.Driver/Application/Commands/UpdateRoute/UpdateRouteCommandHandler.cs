using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.UpdateRoute;

internal sealed class UpdateRouteCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<UpdateRouteCommand, Result>
{
    public async Task<Result> Handle(UpdateRouteCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Routes
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Route.NotFound", "Route not found."));

        entity.Update(request.RouteCode, request.CycleCode);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
