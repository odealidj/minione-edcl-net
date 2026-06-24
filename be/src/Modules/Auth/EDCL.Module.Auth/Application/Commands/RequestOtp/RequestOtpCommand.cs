using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.RequestOtp;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record RequestOtpCommand(
    string PhoneNumber)
    : ICommand<Result<RequestOtpResponse>>;

// ── Response ─────────────────────────────────────────────────────────────────

public sealed record RequestOtpResponse(
    bool Success,
    string Message);
