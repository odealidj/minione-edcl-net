using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Module.Auth.Application.Commands.ChangeDriverPin;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Auth.Controllers;

[ApiController]
[Route("api/v1/auth/drivers")] // Under /auth/ to pass through Gateway's auth-route
public sealed class DriversAuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Authenticates a Driver via Mobile App.
    /// Returns 401 Unauthorized with "Auth.ForceChangePin" if the driver is using the default PIN.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        if (result.IsFailure)
        {
            var errors = new[] { new ApiError("auth", result.Error.Code, result.Error.Message) };
            if (result.Error.Code == "Auth.ForceChangePin")
                return StatusCode(StatusCodes.Status401Unauthorized, ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status401Unauthorized, errors));
                
            return Unauthorized(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status401Unauthorized, errors));
        }

        return Ok(ApiResponse<LoginResponse>.Success(result.Value, traceId));
    }

    /// <summary>
    /// Changes the driver's PIN. 
    /// Can be called without an access token (e.g. during the mandatory change PIN screen after first login).
    /// </summary>
    [HttpPost("change-pin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePin(
        [FromBody] ChangeDriverPinCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        if (result.IsFailure)
        {
            var errors = new[] { new ApiError("auth", result.Error.Code, result.Error.Message) };
            if (result.Error.Code == "Auth.InvalidCredentials")
                return Unauthorized(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status401Unauthorized, errors));

            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status400BadRequest, errors));
        }

        return Ok(ApiResponse<LoginResponse>.Success(result.Value, traceId));
    }

    /// <summary>
    /// Logs out a Driver by revoking their refresh token.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EDCL.Module.Auth.Application.Commands.LogoutDriver.LogoutDriverResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutDriver(
        [FromBody] EDCL.Module.Auth.Application.Commands.LogoutDriver.LogoutDriverCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        // Idempotent logout - always return 200 OK even if token is already revoked
        return Ok(ApiResponse<EDCL.Module.Auth.Application.Commands.LogoutDriver.LogoutDriverResponse>.Success(result.Value, traceId));
    }
}
