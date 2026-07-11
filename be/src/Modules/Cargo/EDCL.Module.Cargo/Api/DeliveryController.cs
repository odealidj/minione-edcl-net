using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace EDCL.Module.Cargo.Api;

[ApiController]
[Route("api/v1/mobile/cargo/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;

    public DeliveryController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost("simulate")]
    public async Task<IActionResult> SimulateDelivery([FromBody] DeliverySimulateRequest request)
    {
        var integrationEvent = new ManifestDeliveredIntegrationEvent
        {
            ManifestId = request.ManifestId,
            ManifestNo = request.ManifestNo,
            Status = request.Status,
            DeliveredAt = DateTime.UtcNow,
            Remarks = request.Remarks
        };

        // Publish event to RabbitMQ
        await _publishEndpoint.Publish(integrationEvent);

        return Accepted(new { Message = "Delivery simulation event published.", Event = integrationEvent });
    }
}

public class DeliverySimulateRequest
{
    public long ManifestId { get; set; }
    public string ManifestNo { get; set; } = string.Empty;
    public string Status { get; set; } = "Delivered";
    public string Remarks { get; set; } = "Simulation of delivery from EDCL";
}
