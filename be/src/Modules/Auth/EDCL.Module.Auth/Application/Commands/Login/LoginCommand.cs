using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.Login;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record LoginCommand(
    string PhoneNumber,
    string Pin,
    string? DeviceInfo = null)
    : ICommand<Result<LoginResponse>>;

// ── Response ─────────────────────────────────────────────────────────────────

public sealed record LoginResponse(
    long DriverId,
    string Name,
    string Nik,
    string? PhotoUrl,
    string? LogisticPartnerName,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
