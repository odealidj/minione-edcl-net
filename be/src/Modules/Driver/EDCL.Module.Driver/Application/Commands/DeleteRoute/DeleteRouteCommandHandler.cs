using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.DeleteRoute;

internal sealed class DeleteRouteCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<DeleteRouteCommand, Result>
{
    public async Task<Result> Handle(DeleteRouteCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Routes
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Route.NotFound", "Route not found."));

        entity.SoftDelete("SYSTEM");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
