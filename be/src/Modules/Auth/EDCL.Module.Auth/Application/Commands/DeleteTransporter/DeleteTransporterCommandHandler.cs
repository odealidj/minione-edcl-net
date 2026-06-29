using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Auth.Application.Commands.DeleteTransporter;

internal sealed class DeleteTransporterCommandHandler(AuthDbContext dbContext) 
    : IRequestHandler<DeleteTransporterCommand, Result>
{
    public async Task<Result> Handle(DeleteTransporterCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Transporters
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Transporter.NotFound", "Transporter not found."));

        entity.SoftDelete("SYSTEM");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
