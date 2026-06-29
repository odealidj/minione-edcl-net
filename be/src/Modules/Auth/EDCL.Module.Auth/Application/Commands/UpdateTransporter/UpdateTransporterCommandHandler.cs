using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Auth.Application.Commands.UpdateTransporter;

internal sealed class UpdateTransporterCommandHandler(AuthDbContext dbContext) 
    : IRequestHandler<UpdateTransporterCommand, Result>
{
    public async Task<Result> Handle(UpdateTransporterCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Transporters
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Transporter.NotFound", "Transporter not found."));

        typeof(EDCL.Module.Auth.Domain.Entities.Transporter).GetProperty("Name")!.SetValue(entity, request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
