using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Auth.Application.Commands.LogoutAppUser;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record LogoutAppUserCommand(string RefreshToken) 
    : ICommand<Result<LogoutAppUserResponse>>;

// ── Response ─────────────────────────────────────────────────────────────────

public sealed record LogoutAppUserResponse(bool Success);

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class LogoutAppUserCommandHandler(
    AuthDbContext dbContext)
    : IRequestHandler<LogoutAppUserCommand, Result<LogoutAppUserResponse>>
{
    public async Task<Result<LogoutAppUserResponse>> Handle(LogoutAppUserCommand request, CancellationToken cancellationToken)
    {
        // Find the active refresh token
        var tokenEntity = await dbContext.AppUserRefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked, cancellationToken);

        if (tokenEntity is null)
        {
            // If token is invalid or already revoked, just return success (idempotent logout)
            return Result<LogoutAppUserResponse>.Success(new LogoutAppUserResponse(true));
        }

        tokenEntity.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LogoutAppUserResponse>.Success(new LogoutAppUserResponse(true));
    }
}
