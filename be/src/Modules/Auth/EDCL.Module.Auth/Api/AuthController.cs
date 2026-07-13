using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Auth.Api;

[ApiController]
[Route("api/v1/web/auth/staff")]
[Produces("application/json", "application/x-msgpack")]
public sealed class AuthController(ISender mediator) : ControllerBase
{
    /// <summary>Meminta pengiriman kode OTP ke nomor HP.</summary>
    [HttpPost("request-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> RequestOtp(
        [FromBody] Application.Commands.RequestOtp.RequestOtpCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        return result.Match<IActionResult>(
            onSuccess: _ => Ok(ApiResponse<object?>.Success(null, traceId, message: "OTP sent.")),
            onFailure: error => BadRequest(ApiResponse<object>.Fail(error.Message, traceId, 400)));
    }

    /// <summary>Login driver menggunakan nomor HP dan PIN atau OTP.</summary>
    /// <remarks>
    /// Mengembalikan access token (15 menit) dan refresh token (30 hari).
    /// Simpan refresh token di secure storage dan gunakan untuk memperbarui
    /// access token sebelum kadaluarsa.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 422)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            PhoneNumber: request.PhoneNumber,
            Pin: request.Pin,
            DeviceInfo: Request.Headers.UserAgent.ToString());

        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        return result.Match<IActionResult>(
            onSuccess: data => Ok(ApiResponse<LoginResponse>.Success(data, traceId)),
            onFailure: error => error.Type switch
            {
                Shared.Kernel.Common.ErrorType.Validation   => UnprocessableEntity(
                    ApiResponse<object>.Fail(error.Message, traceId, 422)),
                Shared.Kernel.Common.ErrorType.Unauthorized => Unauthorized(
                    ApiResponse<object>.Fail(error.Message, traceId, 401)),
                _ => StatusCode(500, ApiResponse<object>.Fail("Internal error.", traceId, 500))
            });
    }

    /// <summary>Login admin/staff menggunakan Email dan Password.</summary>
    [HttpPost("admin/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginAppUserResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 422)]
    public async Task<IActionResult> AdminLogin(
        [FromBody] LoginAppUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginAppUserCommand(request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        return result.Match<IActionResult>(
            onSuccess: data => Ok(ApiResponse<LoginAppUserResponse>.Success(data, traceId)),
            onFailure: error => error.Type switch
            {
                Shared.Kernel.Common.ErrorType.Validation => UnprocessableEntity(ApiResponse<object>.Fail(error.Message, traceId, 422)),
                Shared.Kernel.Common.ErrorType.Unauthorized => Unauthorized(ApiResponse<object>.Fail(error.Message, traceId, 401)),
                _ => StatusCode(500, ApiResponse<object>.Fail("Internal error.", traceId, 500))
            });
    }

    /// <summary>Perbarui access token menggunakan refresh token.</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TokenPairResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Application.Commands.RefreshToken.RefreshTokenCommand(
            RawRefreshToken: request.RefreshToken,
            DeviceInfo: Request.Headers.UserAgent.ToString());

        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        return result.Match<IActionResult>(
            onSuccess: data => Ok(ApiResponse<TokenPairResponse>.Success(data, traceId)),
            onFailure: error => Unauthorized(
                ApiResponse<object>.Fail(error.Message, traceId, 401)));
    }

    /// <summary>Logout — revoke refresh token aktif.</summary>
    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> RevokeToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Application.Commands.RevokeToken.RevokeTokenCommand(request.RefreshToken);
        var result  = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        return result.Match<IActionResult>(
            onSuccess: _ => Ok(ApiResponse<object?>.Success(null, traceId, message: "Token revoked.")),
            onFailure: error => Unauthorized(
                ApiResponse<object>.Fail(error.Message, traceId, 401)));
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────
public sealed record LoginRequest(string PhoneNumber, string Pin);
public sealed record LoginAppUserRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record TokenPairResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);
