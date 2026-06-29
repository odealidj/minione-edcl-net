using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Queries.GetSupplierById;

internal sealed class GetSupplierByIdQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetSupplierByIdQuery, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (x is null) return Result<SupplierDto>.Failure(Error.NotFound("Supplier.NotFound", "Supplier not found."));

        return Result<SupplierDto>.Success(new SupplierDto(x.Id, x.SupplierCode, x.Name, x.Address, x.Latitude, x.Longitude, x.GeofenceRadiusMeters, x.IsActive));
    }
}
