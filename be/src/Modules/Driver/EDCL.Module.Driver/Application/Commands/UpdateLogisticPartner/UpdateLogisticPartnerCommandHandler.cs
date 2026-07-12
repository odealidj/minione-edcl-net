using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.UpdateLogisticPartner;

internal sealed class UpdateLogisticPartnerCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<UpdateLogisticPartnerCommand, Result>
{
    public async Task<Result> Handle(UpdateLogisticPartnerCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.LogisticPartners
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("LogisticPartner.NotFound", "LogisticPartner not found."));

        entity.Update(request.Code, request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
