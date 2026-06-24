using EDCL.Module.Auth.Application.Ports;
using EDCL.Shared.Http.Behaviors;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RawRefreshToken)
    : ICommand<Result<bool>>;

public sealed class RevokeTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<RevokeTokenCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RevokeTokenCommand request,
        CancellationToken cancellationToken)
    {
        var hashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(request.RawRefreshToken));
        var hashedToken = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var storedToken = await refreshTokenRepository
            .FindByHashedTokenAsync(hashedToken, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked)
            return true; // idempotent — sudah revoked = success

        storedToken.Revoke();
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
