using EDCL.Module.Auth.Application.Ports;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Auth.Application.Commands.UpdateAppUserRole;

public sealed class UpdateAppUserRoleCommandHandler(
    IAppUserRepository appUserRepository,
    IRoleRepository roleRepository,
    ILogger<UpdateAppUserRoleCommandHandler> logger)
    : IRequestHandler<UpdateAppUserRoleCommand, Result<UpdateAppUserRoleResponse>>
{
    public async Task<Result<UpdateAppUserRoleResponse>> Handle(
        UpdateAppUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check if user exists
        var user = await appUserRepository.FindByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("AppUser", request.UserId.ToString());
        }

        // 2. Check if the new role exists
        var role = await roleRepository.FindByCodeAsync(request.RoleCode, cancellationToken);
        if (role is null)
        {
            return Error.NotFound("Role", request.RoleCode);
        }

        // 3. Update User Role
        user.ChangeRole(role.Id);

        // 4. Save Changes
        await appUserRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AppUser {UserId} role updated to {RoleCode} successfully.", user.Id, role.Code);

        return new UpdateAppUserRoleResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email,
            RoleCode: role.Code);
    }
}
