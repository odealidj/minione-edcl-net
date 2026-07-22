using EDCL.Module.Cargo.Application.Queries.AdminGetIdcsDeliveries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Cargo.Api;

[ApiController]
[Route("api/v1/admin/cargo/idcs-deliveries")]
[Authorize(Roles = "ADMIN")]
public class IdcsDeliveryController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetIdcsDeliveries([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await mediator.Send(new AdminGetIdcsDeliveriesQuery(search, page, pageSize));
        return Ok(result);
    }
}
