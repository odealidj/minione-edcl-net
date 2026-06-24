namespace EDCL.Worker.Outbox;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Worker started. Polling [outbox].[messages] table...");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Outbox Worker is active at: {time}", DateTimeOffset.Now);
            }

            // Simulate polling DB and publishing to RabbitMQ
            await Task.Delay(5000, stoppingToken);
        }
    }
}
