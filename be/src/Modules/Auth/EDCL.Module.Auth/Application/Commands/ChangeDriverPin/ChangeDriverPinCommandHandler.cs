using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Module.Auth.Application.Ports;
using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Kernel.Ports;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Auth.Application.Commands.ChangeDriverPin;

public sealed class ChangeDriverPinCommandHandler(
    IDriverRepository driverRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtService,
    ILogger<ChangeDriverPinCommandHandler> logger)
    : IRequestHandler<ChangeDriverPinCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        ChangeDriverPinCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Find active driver ──────────────────────────────────────────
        var driver = await driverRepository.FindActiveByPhoneAsync(
            request.PhoneNumber, cancellationToken);

        if (driver is null)
            return Error.NotFound("Driver", request.PhoneNumber);

        // ── 2. Verify Old PIN ─────────────────────────────────────────────
        bool isOldPinValid = false;
        try
        {
            isOldPinValid = BCrypt.Net.BCrypt.Verify(request.OldPin, driver.PinHash);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify old PIN hash for driver {DriverId}", driver.Id);
        }

        if (!isOldPinValid)
        {
            logger.LogWarning("Failed change PIN attempt for phone: {Phone}. Incorrect old PIN.", request.PhoneNumber);
            return Error.Unauthorized("Auth.InvalidCredentials", "Nomor HP atau PIN lama tidak valid.");
        }

        // ── 3. Update PIN and MustChangePin flag ──────────────────────────
        string newPinHash = BCrypt.Net.BCrypt.HashPassword(request.NewPin, 9);
        driver.ChangePin(newPinHash);

        await driverRepository.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Driver {DriverId} successfully changed their PIN.", driver.Id);

        // ── 4. Issue Access Token (Auto Login) ────────────────────────────
        var (accessToken, accessExpiry) = jwtService.GenerateAccessToken(driver);

        // ── 5. Issue Refresh Token (rotate: revoke old if exists) ─────────
        var existingTokens = await refreshTokenRepository
            .GetActiveTokensByDriverAsync(driver.Id, cancellationToken);

        foreach (var old in existingTokens.Where(t => t.DeviceInfo == request.DeviceInfo && t.IsActive))
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

        return new LoginResponse(
            DriverId: driver.Id,
            Name: driver.Name,
            Nik: driver.Nik,
            PhotoUrl: driver.PhotoUrl,
            TransporterName: driver.Transporter?.Name,
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            AccessTokenExpiresAt: accessExpiry,
            RefreshTokenExpiresAt: DateTime.UtcNow.AddDays(jwtService.RefreshTokenExpiryDays));
    }
}
