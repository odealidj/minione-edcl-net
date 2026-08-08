using EDCL.Shared.Kernel.Events;
using EDCL.Worker.GpsTracker.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Consumers;

public class SimulationResetIntegrationEventConsumer(
    SimulationSessionManager sessionManager,
    ILogger<SimulationResetIntegrationEventConsumer> logger) : IConsumer<SimulationResetIntegrationEvent>
{
    public Task Consume(ConsumeContext<SimulationResetIntegrationEvent> context)
    {
        logger.LogInformation("Received SimulationResetIntegrationEvent. Clearing all simulation sessions...");
        sessionManager.ClearSessions();
        return Task.CompletedTask;
    }
}
