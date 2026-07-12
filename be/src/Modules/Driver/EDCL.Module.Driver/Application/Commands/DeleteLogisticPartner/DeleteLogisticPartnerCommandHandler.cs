using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.DeleteLogisticPartner;

internal sealed class DeleteLogisticPartnerCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<DeleteLogisticPartnerCommand, Result>
{
    public async Task<Result> Handle(DeleteLogisticPartnerCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.LogisticPartners
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("LogisticPartner.NotFound", "LogisticPartner not found."));

        entity.SoftDelete("SYSTEM");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
