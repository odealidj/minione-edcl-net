namespace EDCL.Worker.Ingestion;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Ingestion Worker started. Listening to 'edcl.manifest.ingested' queue...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Ingestion Worker is active at: {time}", DateTimeOffset.Now);
            }

            // Simulate listening to RabbitMQ and writing to SQL
            await Task.Delay(10000, stoppingToken);
        }
    }
}
