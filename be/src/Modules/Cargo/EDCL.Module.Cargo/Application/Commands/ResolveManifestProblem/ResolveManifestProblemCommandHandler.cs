using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Cargo.Application.Commands.ResolveManifestProblem;

using EDCL.Shared.Infrastructure.Persistence;

internal sealed class ResolveManifestProblemCommandHandler(CargoDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<ResolveManifestProblemCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ResolveManifestProblemCommand request, CancellationToken cancellationToken)
    {
        var problem = await dbContext.ManifestProblems
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (problem == null)
            return Result<bool>.Failure(Error.NotFound("ManifestProblem", request.Id));

        var resolvedBy = currentUserService.AuditIdentity ?? "System";
        problem.Resolve("Resolved", resolvedBy, request.Reason);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
