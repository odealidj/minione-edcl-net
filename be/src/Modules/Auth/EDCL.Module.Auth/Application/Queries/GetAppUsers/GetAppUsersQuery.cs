using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Queries.GetAppUsers;

public sealed record GetAppUsersQuery(string? Search = null, int PageNumber = 1, int PageSize = 10) 
    : IRequest<Result<GetAppUsersResponse>>;

public sealed record GetAppUsersResponse(
    IReadOnlyList<AppUserDto> Items,
    long TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public sealed record AppUserDto(
    long Id,
    string Name,
    string Email,
    string RoleCode);
