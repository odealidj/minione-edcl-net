using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.CreateTransporter;

internal sealed class CreateTransporterCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<CreateTransporterCommand, Result<long>>
{
    public async Task<Result<long>> Handle(CreateTransporterCommand request, CancellationToken cancellationToken)
    {
        var entity = (EDCL.Module.Driver.Domain.Entities.Transporter)System.Activator.CreateInstance(typeof(EDCL.Module.Driver.Domain.Entities.Transporter), true);
        typeof(EDCL.Module.Driver.Domain.Entities.Transporter).GetProperty("Name")!.SetValue(entity, request.Name);
        dbContext.Transporters.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<long>.Success(entity.Id);
    }
}
