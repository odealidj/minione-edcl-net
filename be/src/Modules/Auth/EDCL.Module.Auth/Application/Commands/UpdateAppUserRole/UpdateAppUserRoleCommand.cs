using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;

namespace EDCL.Module.Auth.Application.Commands.UpdateAppUserRole;

public sealed record UpdateAppUserRoleCommand(
    long UserId,
    string RoleCode)
    : ICommand<Result<UpdateAppUserRoleResponse>>;

public sealed record UpdateAppUserRoleResponse(
    long Id,
    string Name,
    string Email,
    string RoleCode);
