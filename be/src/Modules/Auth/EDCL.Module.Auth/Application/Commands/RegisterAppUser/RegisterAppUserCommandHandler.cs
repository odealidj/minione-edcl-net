using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Auth.Application.Commands.RegisterAppUser;

public sealed class RegisterAppUserCommandHandler(
    IAppUserRepository appUserRepository,
    IRoleRepository roleRepository,
    ILogger<RegisterAppUserCommandHandler> logger)
    : IRequestHandler<RegisterAppUserCommand, Result<RegisterAppUserResponse>>
{
    public async Task<Result<RegisterAppUserResponse>> Handle(
        RegisterAppUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify Role exists (Default to USER)
        var roleCode = "USER";
        var role = await roleRepository.FindByCodeAsync(roleCode, cancellationToken);
        if (role is null)
        {
            return Error.NotFound("Role", roleCode);
        }

        // 2. Verify Email is unique
        var existingUser = await appUserRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Error.Conflict("AppUser.EmailInUse", "The provided email is already in use.");
        }

        // 3. Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 4. Create User
        var appUser = AppUser.Create(
            name: request.Name,
            email: request.Email,
            passwordHash: passwordHash,
            roleId: role.Id);

        // 5. Save to DB
        await appUserRepository.AddAsync(appUser, cancellationToken);
        await appUserRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("AppUser {AppUserId} ({Name}) registered successfully.", appUser.Id, appUser.Name);

        return new RegisterAppUserResponse(
            Id: appUser.Id,
            Name: appUser.Name,
            Email: appUser.Email,
            RoleCode: role.Code);
    }
}
