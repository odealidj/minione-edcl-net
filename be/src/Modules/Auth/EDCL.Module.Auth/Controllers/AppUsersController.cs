using EDCL.Shared.Kernel.Common;
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

    /// <summary>
    /// Logs out an AppUser by revoking their refresh token.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<EDCL.Module.Auth.Application.Commands.LogoutAppUser.LogoutAppUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAppUser(
        [FromBody] EDCL.Module.Auth.Application.Commands.LogoutAppUser.LogoutAppUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

    // Idempotent logout - always return 200 OK even if token is already revoked
        return Ok(ApiResponse<EDCL.Module.Auth.Application.Commands.LogoutAppUser.LogoutAppUserResponse>.Success(result.Value, traceId));
    }

    /// <summary>
    /// Updates the role of an AppUser. Only accessible by ADMIN.
    /// </summary>
    [HttpPut("{userId}/role")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<EDCL.Module.Auth.Application.Commands.UpdateAppUserRole.UpdateAppUserRoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(
        [FromRoute] long userId,
        [FromBody] EDCL.Module.Auth.Application.Commands.UpdateAppUserRole.UpdateAppUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        // Ensure route ID matches body ID
        if (userId != command.UserId)
        {
            var traceId = HttpContext.GetTraceId();
            return BadRequest(ApiResponse<object>.Fail("User ID mismatch.", traceId, StatusCodes.Status400BadRequest));
        }

        var result = await mediator.Send(command, cancellationToken);
        var traceIdSuccess = HttpContext.GetTraceId();

        if (result.IsFailure)
        {
            if (result.Error.Code == "AppUser.NotFound" || result.Error.Code == "Role.NotFound")
                return NotFound(ApiResponse<object>.Fail(result.Error.Message, traceIdSuccess, StatusCodes.Status404NotFound));
                
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceIdSuccess, StatusCodes.Status400BadRequest));
        }

        return Ok(ApiResponse<EDCL.Module.Auth.Application.Commands.UpdateAppUserRole.UpdateAppUserRoleResponse>.Success(result.Value, traceIdSuccess));
    }

    /// <summary>
    /// Gets a paginated list of AppUsers. Only accessible by ADMIN.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EDCL.Module.Auth.Application.Queries.GetAppUsers.AppUserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var command = new EDCL.Module.Auth.Application.Queries.GetAppUsers.GetAppUsersQuery(search, page, pageSize);
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        if (result.IsFailure)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status400BadRequest));
        }

        var pagination = PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount);
        return Ok(ApiResponse<IReadOnlyList<EDCL.Module.Auth.Application.Queries.GetAppUsers.AppUserDto>>.Paginated(result.Value.Items, pagination, traceId));
    }
}
