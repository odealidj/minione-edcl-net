using EDCL.Shared.Kernel.Common;
using MediatR;

namespace EDCL.Module.Auth.Application.Commands.Login;

public sealed record LoginAppUserCommand(string Email, string Password) : IRequest<Result<LoginAppUserResponse>>;

public sealed record LoginAppUserResponse(string AccessToken, DateTime ExpiresAt, string RefreshToken);

