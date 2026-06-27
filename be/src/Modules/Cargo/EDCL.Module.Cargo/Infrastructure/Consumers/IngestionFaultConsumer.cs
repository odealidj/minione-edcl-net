using System.Text.Json;
using EDCL.Module.Cargo.Domain.Entities;
using EDCL.Module.Cargo.Domain.Events;
using EDCL.Module.Cargo.Infrastructure.Channels;
using EDCL.Module.Cargo.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class IngestionFaultConsumer :
    IConsumer<IngestionErrorEvent>
{
    private readonly ILogger<IngestionFaultConsumer> _logger;
    private readonly IngestionErrorChannel _errorChannel;
    private readonly IServiceProvider _serviceProvider;

    public IngestionFaultConsumer(
        ILogger<IngestionFaultConsumer> logger,
        IngestionErrorChannel errorChannel,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _errorChannel = errorChannel;
        _serviceProvider = serviceProvider;
    }

    public async Task Consume(ConsumeContext<IngestionErrorEvent> context)
    {
        var ev = context.Message;
        _logger.LogError("Received ingestion fault for {EventType}. Exception: {ErrorMessage}", ev.EventType, ev.ErrorMessage);

        var error = new IngestionError
        {
            EventType = ev.EventType,
            Payload = ev.Payload,
            ErrorMessage = ev.ErrorMessage,
            StackTrace = ev.StackTrace,
            OccurredAt = ev.OccurredAt,
            IsResolved = false
        };

        // Saving to DB
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CargoDbContext>();
            dbContext.IngestionErrors.Add(error);
            await dbContext.SaveChangesAsync();
        }

        // Push to SSE Channel
        await _errorChannel.PublishErrorAsync(error);
    }
}
