using EDCL.Module.Auth.Application.Commands.RegisterAppUser;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Auth.Controllers;

[ApiController]
[Route("api/v1/auth/users")]
public sealed class AppUsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Registers a new AppUser (Admin/Web User).
    /// </summary>
    /// <remarks>
    /// Currently allows anonymous access to facilitate initial setup.
    /// Once the first admin is created, this should be restricted using [Authorize(Roles = "ADMIN")].
    /// </remarks>
    [HttpPost("register")]
    [AllowAnonymous] // TODO: Change to [Authorize(Roles = "ADMIN")] after initial setup
    [ProducesResponseType(typeof(ApiResponse<RegisterAppUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterAppUser(
        [FromBody] RegisterAppUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        if (result.IsFailure)
        {
            if (result.Error.Code == "AppUser.EmailInUse")
                return Conflict(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status409Conflict));
                
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status400BadRequest));
        }

        return Created(string.Empty, ApiResponse<RegisterAppUserResponse>.Created(result.Value, traceId));
    }

    /// <summary>
    /// Authenticates an AppUser and returns JWT tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EDCL.Module.Auth.Application.Commands.Login.LoginAppUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginAppUser(
        [FromBody] EDCL.Module.Auth.Application.Commands.Login.LoginAppUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status400BadRequest));

        return Ok(ApiResponse<EDCL.Module.Auth.Application.Commands.Login.LoginAppUserResponse>.Success(result.Value, traceId));
    }
}
