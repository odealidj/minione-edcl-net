using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Api;
using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.RefreshToken;

// ── Command ───────────────────────────────────────────────────────────────────
public sealed record RefreshTokenCommand(
    string RawRefreshToken,
    string? DeviceInfo = null)
    : ICommand<Result<TokenPairResponse>>;

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtService)
    : IRequestHandler<RefreshTokenCommand, Result<TokenPairResponse>>
{
    public async Task<Result<TokenPairResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Hash the raw token to find it in DB ────────────────────────
        var hashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(request.RawRefreshToken));
        var hashedToken = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var storedToken = await refreshTokenRepository
            .FindByHashedTokenAsync(hashedToken, cancellationToken);

        if (storedToken is null || !storedToken.IsActive || storedToken.Driver is null)
            return Error.Unauthorized("Auth.InvalidRefreshToken",
                "Refresh token tidak valid atau sudah kadaluarsa.");

        // ── 2. Rotate: revoke old token ───────────────────────────────────
        var (newRawToken, newHashedToken) = jwtService.GenerateRefreshToken();
        storedToken.Revoke(replacedBy: newHashedToken);

        // ── 3. Issue new refresh token ────────────────────────────────────
        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            driverId: storedToken.DriverId,
            hashedToken: newHashedToken,
            expiryDays: jwtService.RefreshTokenExpiryDays,
            deviceInfo: request.DeviceInfo ?? storedToken.DeviceInfo);

        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        // ── 4. Issue new access token ─────────────────────────────────────
        var (accessToken, accessExpiry) = jwtService.GenerateAccessToken(storedToken.Driver);

        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenPairResponse(
            AccessToken: accessToken,
            RefreshToken: newRawToken,
            AccessTokenExpiresAt: accessExpiry,
            RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(jwtService.RefreshTokenExpiryDays));
    }
}
