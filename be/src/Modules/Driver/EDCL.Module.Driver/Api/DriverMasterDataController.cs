using EDCL.Shared.Kernel.Common;
using EDCL.Shared.Http.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Driver.Api;

[ApiController]
[Route("api/v1/master/trucks")]
[Tags("Admin Master Data (Driver)")]
[Authorize(Roles = "ADMIN")]
public class AdminTrucksController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Application.DTOs.TruckDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.GetTrucks.GetTrucksQuery(search, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<Application.DTOs.TruckDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Queries.GetTruckById.GetTruckByIdQuery(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<Application.DTOs.TruckDto>.Success(result.Value, traceId))
            : NotFound(ApiResponse<object>.Fail(result.Error.Message, traceId, 404));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Application.Commands.CreateTruck.CreateTruckCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<long>.Success(result.Value, traceId, 201))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] Application.Commands.UpdateTruck.UpdateTruckCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest();
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Commands.DeleteTruck.DeleteTruckCommand(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }
}

[ApiController]
[Route("api/v1/master/suppliers")]
[Tags("Admin Master Data (Driver)")]
[Authorize(Roles = "ADMIN")]
public class AdminSuppliersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<Application.DTOs.SupplierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new Application.Queries.GetSuppliers.GetSuppliersQuery(search, page, pageSize), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<IReadOnlyList<Application.DTOs.SupplierDto>>.Paginated(result.Value.Items, PaginationMeta.From(result.Value.PageNumber, result.Value.PageSize, result.Value.TotalCount), traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Queries.GetSupplierById.GetSupplierByIdQuery(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<Application.DTOs.SupplierDto>.Success(result.Value, traceId))
            : NotFound(ApiResponse<object>.Fail(result.Error.Message, traceId, 404));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Application.Commands.CreateSupplier.CreateSupplierCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<long>.Success(result.Value, traceId, 201))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] Application.Commands.UpdateSupplier.UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest();
        var result = await mediator.Send(command, cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Application.Commands.DeleteSupplier.DeleteSupplierCommand(id), cancellationToken);
        var traceId = HttpContext.TraceIdentifier;
        return result.IsSuccess 
            ? Ok(ApiResponse<object>.Success(null, traceId))
            : BadRequest(ApiResponse<object>.Fail(result.Error.Message, traceId, 400));
    }
}
