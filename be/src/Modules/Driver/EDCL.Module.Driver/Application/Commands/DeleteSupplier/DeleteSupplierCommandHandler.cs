using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Commands.DeleteSupplier;

internal sealed class DeleteSupplierCommandHandler(DriverDbContext dbContext) 
    : IRequestHandler<DeleteSupplierCommand, Result>
{
    public async Task<Result> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Suppliers
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (entity is null) return Result.Failure(Error.NotFound("Supplier.NotFound", "Supplier not found."));

        entity.SoftDelete("SYSTEM");
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
