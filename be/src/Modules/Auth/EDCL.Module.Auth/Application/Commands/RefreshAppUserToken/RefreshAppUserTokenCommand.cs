using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Auth.Application.Commands.RefreshAppUserToken;

// ── Command ───────────────────────────────────────────────────────────────────
public sealed record RefreshAppUserTokenCommand(
    string RawRefreshToken,
    string? DeviceInfo = null)
    : ICommand<Result<LoginAppUserResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
internal sealed class RefreshAppUserTokenCommandHandler(
    AuthDbContext dbContext,
    IJwtTokenService jwtService)
    : IRequestHandler<RefreshAppUserTokenCommand, Result<LoginAppUserResponse>>
{
    public async Task<Result<LoginAppUserResponse>> Handle(
        RefreshAppUserTokenCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Hash incoming token to match DB storage
        var hashedIncomingToken = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(request.RawRefreshToken));

        // 2. Look up active refresh token belonging to AppUser
        var tokenEntity = await dbContext.AppUserRefreshTokens
            .Include(rt => rt.AppUser)
            .FirstOrDefaultAsync(rt => rt.Token == hashedIncomingToken && !rt.IsRevoked, cancellationToken);

        if (tokenEntity is null || tokenEntity.ExpiresAt <= DateTime.UtcNow || tokenEntity.AppUser is null)
        {
            return Error.Unauthorized("Auth.InvalidRefreshToken",
                "Refresh token tidak valid atau sudah kadaluarsa.");
        }

        // 3. Revoke the used token (Token Rotation for security)
        tokenEntity.Revoke("Refreshed");

        // 4. Issue new tokens
        var newJwt = jwtService.GenerateAccessToken(tokenEntity.AppUser);
        var (newRawToken, newHashedToken) = jwtService.GenerateRefreshToken();

        var newRefreshToken = AppUserRefreshToken.Create(
            appUserId: tokenEntity.AppUser.Id,
            hashedToken: newHashedToken,
            expiryDays: jwtService.RefreshTokenExpiryDays,
            deviceInfo: request.DeviceInfo ?? "Unknown (Refresh)");

        dbContext.AppUserRefreshTokens.Add(newRefreshToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        // 5. Return the new pair
        return Result<LoginAppUserResponse>.Success(
            new LoginAppUserResponse(
                AccessToken: newJwt.AccessToken,
                ExpiresAt: newJwt.ExpiresAt,
                RefreshToken: newRawToken
            ));
    }
}
