using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.CreateRoute;

internal sealed class CreateRouteCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<CreateRouteCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var entity = EDCL.Module.Driver.Domain.Entities.Route.Create(request.RouteCode, request.CycleCode);
        dbContext.Routes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(entity.Id);
    }
}
