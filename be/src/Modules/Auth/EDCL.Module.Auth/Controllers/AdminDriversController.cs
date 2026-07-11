using EDCL.Shared.Kernel.Common;
using EDCL.Module.Auth.Application.Commands.CreateDriver;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Auth.Controllers;

[ApiController]
[Route("api/v1/web/master/drivers")]
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

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EDCL.Module.Auth.Application.DTOs.DriverDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.GetDrivers.GetDriversQuery(search, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<EDCL.Module.Auth.Application.DTOs.DriverDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Queries.GetDriverById.GetDriverByIdQuery(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<EDCL.Module.Auth.Application.DTOs.DriverDto>.Success(result.Value, traceId))
            : NotFound(ApiResponse<object>.Fail(result.Error.Message, traceId, 404));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(long id, [FromBody] Application.Commands.UpdateDriver.UpdateDriverCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest();
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Commands.DeleteDriver.DeleteDriverCommand(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }
}
