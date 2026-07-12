using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.CreateLogisticPartner;

internal sealed class CreateLogisticPartnerCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<CreateLogisticPartnerCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateLogisticPartnerCommand request, CancellationToken cancellationToken)
    {
        var entity = EDCL.Module.Driver.Domain.Entities.LogisticPartner.Create(request.Code, request.Name);
        dbContext.LogisticPartners.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(entity.Id);
    }
}
