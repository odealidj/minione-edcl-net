using EDCL.Shared.Kernel.Common;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Driver.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Queries.GetLogisticPartnerById;

internal sealed class GetLogisticPartnerByIdQueryHandler(DriverDbContext dbContext) 
    : IRequestHandler<GetLogisticPartnerByIdQuery, Result<LogisticPartnerDto>>
{
    public async Task<Result<LogisticPartnerDto>> Handle(GetLogisticPartnerByIdQuery request, CancellationToken cancellationToken)
    {
        var x = await dbContext.LogisticPartners
            .Include(lp => lp.GpsVendorMappings)
            .FirstOrDefaultAsync(lp => lp.Id == request.Id && !lp.IsDeleted, cancellationToken);
            
        if (x is null) return Result<LogisticPartnerDto>.Failure(Error.NotFound("LogisticPartner.NotFound", "LogisticPartner not found."));

        return Result<LogisticPartnerDto>.Success(new LogisticPartnerDto(
            x.Id,
            x.Code,
            x.Name,
            x.GpsVendorMappings.Select(m => m.GpsVendorId).ToList()
        ));
    }
}
