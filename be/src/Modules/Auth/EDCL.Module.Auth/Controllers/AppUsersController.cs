using EDCL.Module.Auth.Application.Commands.RegisterAppUser;
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
    [ProducesResponseType(typeof(RegisterAppUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAppUser(
        [FromBody] RegisterAppUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Code == "AppUser.EmailInUse")
                return Conflict(result.Error);
                
            return BadRequest(result.Error);
        }

        return Created(string.Empty, result.Value);
    }

    /// <summary>
    /// Authenticates an AppUser and returns JWT tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EDCL.Module.Auth.Application.Commands.Login.LoginAppUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAppUser(
        [FromBody] EDCL.Module.Auth.Application.Commands.Login.LoginAppUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}
