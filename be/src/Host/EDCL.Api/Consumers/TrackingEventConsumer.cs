using EDCL.Api.Hubs;
using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace EDCL.Api.Consumers;

public class TrackingEventConsumer(IHubContext<TrackingHub> hubContext) : IConsumer<TruckLocationUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TruckLocationUpdatedIntegrationEvent> context)
    {
        var evt = context.Message;
        
        // Broadcast to all connected clients
        await hubContext.Clients.All.SendAsync("ReceiveLocation", new
        {
            truckId = evt.TruckId,
            latitude = evt.Latitude,
            longitude = evt.Longitude,
            speed = evt.Speed,
            heading = evt.Heading,
            timestamp = evt.Timestamp,
            providerName = evt.ProviderName
        });
    }
}
