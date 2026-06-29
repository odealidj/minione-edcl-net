using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;

namespace EDCL.Module.Auth.Application.Commands.RegisterAppUser;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record RegisterAppUserCommand(
    string Name,
    string Email,
    string Password)
    : ICommand<Result<RegisterAppUserResponse>>;

// ── Response ─────────────────────────────────────────────────────────────────

public sealed record RegisterAppUserResponse(
    long Id,
    string Name,
    string Email,
    string RoleCode);
