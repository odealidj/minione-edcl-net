namespace EDCL.Worker.Reporter;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Reporter Worker started. Listening to 'edcl.job.completed' queue and pushing to IDCS...");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Reporter Worker is active at: {time}", DateTimeOffset.Now);
            }

            // Simulate listening to RabbitMQ and calling IDCS API
            await Task.Delay(15000, stoppingToken);
        }
    }
}
