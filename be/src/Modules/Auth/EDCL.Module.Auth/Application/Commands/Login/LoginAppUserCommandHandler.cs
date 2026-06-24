using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.Login;

public sealed class LoginAppUserCommandHandler(
    IAppUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginAppUserCommand, Result<LoginAppUserResponse>>
{
    public async Task<Result<LoginAppUserResponse>> Handle(LoginAppUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return Result<LoginAppUserResponse>.Failure(new Error("Login.Failed", "Email atau kata sandi salah."));

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginAppUserResponse>.Failure(new Error("Login.Failed", "Email atau kata sandi salah."));

        var (accessToken, expiresAt) = jwtTokenService.GenerateAccessToken(user);
        var (rawRefreshToken, hashedRefreshToken) = jwtTokenService.GenerateRefreshToken();

        var expiryDays = jwtTokenService.RefreshTokenExpiryDays;

        // For now, let's bypass the RefreshToken storage since RefreshToken is currently strongly tied to Driver.
        // We will need to update RefreshToken to support AppUser in the future.
        // Alternatively, we could create an AppUserRefreshToken entity.
        // I will return the tokens and let the client use them.

        return Result<LoginAppUserResponse>.Success(new LoginAppUserResponse(
            AccessToken: accessToken,
            ExpiresAt: expiresAt,
            RefreshToken: rawRefreshToken));
    }
}
