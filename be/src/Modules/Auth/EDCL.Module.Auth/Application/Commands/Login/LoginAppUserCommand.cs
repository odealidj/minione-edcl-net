using EDCL.Shared.Kernel.Common;
using MediatR;
using System.ComponentModel;

namespace EDCL.Module.Auth.Application.Commands.Login;

public sealed record LoginAppUserCommand(
    [property: DefaultValue("admin@edcl.com")] string Email, 
    [property: DefaultValue("Password123!")] string Password) 
    : IRequest<Result<LoginAppUserResponse>>;

public sealed record LoginAppUserResponse(string AccessToken, DateTime ExpiresAt, string RefreshToken);

