using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Auth.Application.Commands.Login;

public sealed class LoginAppUserCommandHandler(
    IAppUserRepository userRepository,
    AuthDbContext dbContext,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginAppUserCommand, Result<LoginAppUserResponse>>
{
    public async Task<Result<LoginAppUserResponse>> Handle(LoginAppUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return Result<LoginAppUserResponse>.Failure(new Error("Login.Failed", "Email atau kata sandi salah."));

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginAppUserResponse>.Failure(new Error("Login.Failed", "Email atau kata sandi salah."));

        var (accessToken, expiresAt) = jwtTokenService.GenerateAccessToken(user);
        var (rawRefreshToken, hashedRefreshToken) = jwtTokenService.GenerateRefreshToken();

        var expiryDays = jwtTokenService.RefreshTokenExpiryDays;

        // Revoke old tokens for this user
        var oldTokens = await dbContext.AppUserRefreshTokens
            .Where(rt => rt.AppUserId == user.Id && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var oldToken in oldTokens)
        {
            oldToken.Revoke();
        }

        var refreshToken = AppUserRefreshToken.Create(
            appUserId: user.Id,
            hashedToken: hashedRefreshToken,
            expiryDays: expiryDays,
            deviceInfo: null); // Could pass from command if needed

        dbContext.AppUserRefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LoginAppUserResponse>.Success(new LoginAppUserResponse(
            AccessToken: accessToken,
            ExpiresAt: expiresAt,
            RefreshToken: rawRefreshToken));
    }
}
