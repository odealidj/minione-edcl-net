using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Auth.Application.Commands.Login;

public sealed class LoginCommandHandler(
    IDriverRepository driverRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtService,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Find active driver by phone number ─────────────────────────
        var driver = await driverRepository.FindActiveByPhoneAsync(
            request.PhoneNumber, cancellationToken);

        if (driver is null)
            return Error.NotFound("Driver", request.PhoneNumber);

        // ── 2. Verify PIN (Permanent via DB Hash) ─────────────────────────
        bool isPinValid = false;
        try
        {
            isPinValid = BCrypt.Net.BCrypt.Verify(request.Pin, driver.PinHash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify PIN hash for driver {DriverId}", driver.Id);
        }

        if (!isPinValid)
        {
            logger.LogWarning(
                "Failed login attempt for phone: {Phone}. Incorrect PIN.", request.PhoneNumber);
            return Error.Unauthorized("Auth.InvalidCredentials",
                "Nomor HP atau PIN tidak valid.");
        }

        // ── 3. Issue Access Token ─────────────────────────────────────────
        var (accessToken, accessExpiry) = jwtService.GenerateAccessToken(driver);

        // ── 4. Issue Refresh Token (rotate: revoke old if exists) ─────────
        var existingTokens = await refreshTokenRepository
            .GetActiveTokensByDriverAsync(driver.Id, cancellationToken);

        // Revoke all existing tokens for this device (best practice: per device)
        foreach (var old in existingTokens
            .Where(t => t.DeviceInfo == request.DeviceInfo && t.IsActive))
        {
            old.Revoke();
        }

        var (rawRefreshToken, hashedRefreshToken) = jwtService.GenerateRefreshToken();
        var refreshToken = Domain.Entities.RefreshToken.Create(
            driverId: driver.Id,
            hashedToken: hashedRefreshToken,
            expiryDays: jwtService.RefreshTokenExpiryDays,
            deviceInfo: request.DeviceInfo);

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Driver {DriverId} ({Name}) logged in successfully.", driver.Id, driver.Name);

        return new LoginResponse(
            DriverId: driver.Id,
            Name: driver.Name,
            Nik: driver.Nik,
            PhotoUrl: driver.PhotoUrl,
            TransporterName: driver.Transporter?.Name,
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,         // Raw token sent to client
            AccessTokenExpiresAt: accessExpiry,
            RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(jwtService.RefreshTokenExpiryDays));
    }
}
