using EDCL.Module.Auth.Application.Commands.CreateDriver;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Auth.Controllers;

[ApiController]
[Route("api/v1/auth/drivers")] // Under /auth/ to pass through Gateway's auth-route
public sealed class AdminDriversController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Registers a new Driver.
    /// </summary>
    /// <remarks>
    /// Only accessible by ADMIN.
    /// Generates a default PIN for the driver upon creation.
    /// </remarks>
    [HttpPost]
    [Authorize] // Requires Authentication (and eventually ADMIN role)
    [ProducesResponseType(typeof(ApiResponse<CreateDriverResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDriver(
        [FromBody] CreateDriverCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.GetTraceId();

        if (result.IsFailure)
        {
            if (result.Error.Code == "Driver.NikInUse" || result.Error.Code == "Driver.PhoneInUse")
                return Conflict(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status409Conflict));
                
            return BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, StatusCodes.Status400BadRequest));
        }

        return Created(string.Empty, ApiResponse<CreateDriverResponse>.Created(result.Value, traceId));
    }
}
