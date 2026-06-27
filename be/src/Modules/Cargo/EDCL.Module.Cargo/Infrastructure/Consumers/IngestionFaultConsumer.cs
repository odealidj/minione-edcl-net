using System.Text.Json;
using EDCL.Module.Cargo.Domain.Entities;
using EDCL.Module.Cargo.Infrastructure.Channels;
using EDCL.Module.Cargo.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class IngestionFaultConsumer :
    IConsumer<Fault<DebeziumManifestEvent>>,
    IConsumer<Fault<DebeziumManifestPartEvent>>,
    IConsumer<Fault<DebeziumManifestKanbanEvent>>,
    IConsumer<Fault<DebeziumManifestSkidEvent>>
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

    public async Task Consume(ConsumeContext<Fault<DebeziumManifestEvent>> context)
    {
        await HandleFaultAsync("DebeziumManifestEvent", context.Message);
    }

    public async Task Consume(ConsumeContext<Fault<DebeziumManifestPartEvent>> context)
    {
        await HandleFaultAsync("DebeziumManifestPartEvent", context.Message);
    }

    public async Task Consume(ConsumeContext<Fault<DebeziumManifestKanbanEvent>> context)
    {
        await HandleFaultAsync("DebeziumManifestKanbanEvent", context.Message);
    }

    public async Task Consume(ConsumeContext<Fault<DebeziumManifestSkidEvent>> context)
    {
        await HandleFaultAsync("DebeziumManifestSkidEvent", context.Message);
    }

    private async Task HandleFaultAsync<T>(string eventType, Fault<T> fault)
    {
        _logger.LogError("Received fault for {EventType}. Exceptions: {Exceptions}", eventType, fault.Exceptions.FirstOrDefault()?.Message);

        var error = new IngestionError
        {
            EventType = eventType,
            Payload = JsonSerializer.Serialize(fault.Message),
            ErrorMessage = fault.Exceptions.FirstOrDefault()?.Message ?? "Unknown Error",
            StackTrace = fault.Exceptions.FirstOrDefault()?.StackTrace,
            OccurredAt = DateTime.UtcNow,
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
