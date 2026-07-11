using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Queries.GetTransporterById;

internal sealed class GetTransporterByIdQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetTransporterByIdQuery, Result<TransporterDto>>
{
    public async Task<Result<TransporterDto>> Handle(GetTransporterByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.Transporters
            .AsNoTracking()
            .FirstOrDefaultAsync(y => y.Id == request.Id && !y.IsDeleted, cancellationToken);
            
        if (x is null) return Result<TransporterDto>.Failure(Error.NotFound("Transporter.NotFound", "Transporter not found."));

        return Result<TransporterDto>.Success(new TransporterDto(x.Id, x.Name));
    }
}
