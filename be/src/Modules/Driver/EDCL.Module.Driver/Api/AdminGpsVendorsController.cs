using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Controllers;

[ApiController]
[Route("api/v1/web/master/gps-vendors")]
[Tags("Admin Master Data (Auth)")]
[Authorize(Roles = "ADMIN")]
public class AdminGpsVendorsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EDCL.Module.Driver.Application.DTOs.GpsVendorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.GetGpsVendors.GetGpsVendorsQuery(search, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<EDCL.Module.Driver.Application.DTOs.GpsVendorDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Queries.GetGpsVendorById.GetGpsVendorByIdQuery(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<EDCL.Module.Driver.Application.DTOs.GpsVendorDto>.Success(result.Value, traceId))
            : NotFound(ApiResponse<object>.Fail(result.Error.Message, traceId, 404));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Application.Commands.CreateGpsVendor.CreateGpsVendorCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<long>.Success(result.Value, traceId, 201))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] Application.Commands.UpdateGpsVendor.UpdateGpsVendorCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest();
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object?>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Commands.DeleteGpsVendor.DeleteGpsVendorCommand(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object?>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] Application.Queries.TestGpsConnection.TestGpsConnectionQuery query, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(query, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<bool>.Success(result.Value, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }
}
