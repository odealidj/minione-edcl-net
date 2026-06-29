using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Auth.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Auth.Application.Queries.GetDriverById;

internal sealed class GetDriverByIdQueryHandler(AuthDbContext dbContext) 
    : IRequestHandler<GetDriverByIdQuery, Result<DriverDto>>
{
    public async Task<Result<DriverDto>> Handle(GetDriverByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (x is null) return Result<DriverDto>.Failure(Error.NotFound("Driver.NotFound", "Driver not found."));

        return Result<DriverDto>.Success(new DriverDto(x.Id, x.Name, x.Nik, x.PhoneNumber, x.IsActive, x.TransporterId));
    }
}
