using EDCL.Module.Auth.Application.Ports;
using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using EDCL.Module.Auth.Infrastructure.Persistence;

namespace EDCL.Module.Auth.Application.Commands.LogoutDriver;

// ── Command ──────────────────────────────────────────────────────────────────

public sealed record LogoutDriverCommand(string RefreshToken) 
    : ICommand<Result<LogoutDriverResponse>>;

// ── Response ─────────────────────────────────────────────────────────────────

public sealed record LogoutDriverResponse(bool Success);

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class LogoutDriverCommandHandler(
    AuthDbContext dbContext)
    : IRequestHandler<LogoutDriverCommand, Result<LogoutDriverResponse>>
{
    public async Task<Result<LogoutDriverResponse>> Handle(LogoutDriverCommand request, CancellationToken cancellationToken)
    {
        // Find the active refresh token
        var tokenEntity = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked, cancellationToken);

        if (tokenEntity is null)
        {
            // If token is invalid or already revoked, just return success (idempotent logout)
            return Result<LogoutDriverResponse>.Success(new LogoutDriverResponse(true));
        }

        tokenEntity.Revoke();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LogoutDriverResponse>.Success(new LogoutDriverResponse(true));
    }
}
