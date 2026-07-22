using MassTransit;
using Microsoft.Extensions.Logging;
using EDCL.Module.Cargo.Domain.Events;
using EDCL.Module.Cargo.Infrastructure.Channels;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class IngestionMetricsConsumer : IConsumer<IngestionMetricsEvent>
{
    private readonly ILogger<IngestionMetricsConsumer> _logger;
    private readonly IngestionMetricsChannel _metricsChannel;

    public IngestionMetricsConsumer(
        ILogger<IngestionMetricsConsumer> logger,
        IngestionMetricsChannel metricsChannel)
    {
        _logger = logger;
        _metricsChannel = metricsChannel;
    }

    public Task Consume(ConsumeContext<IngestionMetricsEvent> context)
    {
        _logger.LogInformation("Received IngestionMetricsEvent for Session ID: {SessionId}, Processed: {Processed}", 
            context.Message.SessionId, context.Message.TotalProcessed);
            
        _metricsChannel.BroadcastMetrics(context.Message);
        
        return Task.CompletedTask;
    }
}
