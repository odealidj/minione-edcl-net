using EDCL.Shared.Kernel.Events;
using EDCL.Worker.GpsTracker.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Consumers;

public class SimulationStartedConsumer(SimulationSessionManager sessionManager, ILogger<SimulationStartedConsumer> logger) : IConsumer<SimulationStartedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<SimulationStartedIntegrationEvent> context)
    {
        logger.LogInformation("Received SimulationStartedIntegrationEvent for Truck {TruckId}", context.Message.TruckId);
        await sessionManager.StartSimulationAsync(context.Message, context.CancellationToken);
    }
}
