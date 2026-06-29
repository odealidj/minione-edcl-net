using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Auth.Application.Commands.DeleteDriver;

internal sealed class DeleteDriverCommandHandler(AuthDbContext dbContext) 
    : IRequestHandler<DeleteDriverCommand, Result>
{
    public async Task<Result> Handle(DeleteDriverCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Drivers
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Driver.NotFound", "Driver not found."));

        entity.SoftDelete("SYSTEM");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
