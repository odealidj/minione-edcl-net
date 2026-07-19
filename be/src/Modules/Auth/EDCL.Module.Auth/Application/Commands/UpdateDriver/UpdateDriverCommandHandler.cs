using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Auth.Application.Commands.UpdateDriver;

internal sealed class UpdateDriverCommandHandler(AuthDbContext dbContext) 
    : IRequestHandler<UpdateDriverCommand, Result>
{
    public async Task<Result> Handle(UpdateDriverCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Drivers
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Driver.NotFound", "Driver not found."));

        typeof(EDCL.Module.Auth.Domain.Entities.Driver).GetProperty("Name")!.SetValue(entity, request.Name);
        typeof(EDCL.Module.Auth.Domain.Entities.Driver).GetProperty("Nik")!.SetValue(entity, request.Nik);
        typeof(EDCL.Module.Auth.Domain.Entities.Driver).GetProperty("PhoneNumber")!.SetValue(entity, request.PhoneNumber);
        typeof(EDCL.Module.Auth.Domain.Entities.Driver).GetProperty("LogisticPartnerId")!.SetValue(entity, request.LogisticPartnerId);
        
        if (request.IsActive) entity.Activate();
        else entity.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
