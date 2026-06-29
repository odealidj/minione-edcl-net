using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Auth.Application.Queries.GetAppUsers;

public sealed class GetAppUsersQueryHandler(AuthDbContext dbContext)
    : IRequestHandler<GetAppUsersQuery, Result<GetAppUsersResponse>>
{
    public async Task<Result<GetAppUsersResponse>> Handle(GetAppUsersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.AppUsers
            .Include(u => u.Role)
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(searchTerm) || u.Email.ToLower().Contains(searchTerm));
        }

        query = query.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (request.PageNumber - 1) * request.PageSize;

        var items = await query
            .OrderBy(u => u.Id)
            .Skip(offset)
            .Take(request.PageSize)
            .Select(u => new AppUserDto(
                u.Id,
                u.Name,
                u.Email,
                u.Role != null ? u.Role.Code : string.Empty
            ))
            .ToListAsync(cancellationToken);

        var totalPages = request.PageSize > 0 
            ? (int)Math.Ceiling(totalCount / (double)request.PageSize) 
            : 0;

        var paginatedResult = new GetAppUsersResponse(
            items.AsReadOnly(),
            totalCount,
            request.PageNumber,
            request.PageSize,
            totalPages);

        return paginatedResult;
    }
}
