using System.Data;
using Dapper;
using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.Reporter.Consumers;

public class ManifestDeliveredConsumer : IConsumer<ManifestDeliveredIntegrationEvent>
{
    private readonly ILogger<ManifestDeliveredConsumer> _logger;
    private readonly string _idcsConnectionString;

    public ManifestDeliveredConsumer(ILogger<ManifestDeliveredConsumer> logger, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _logger = logger;
        _idcsConnectionString = configuration.GetConnectionString("IdcsDb") 
            ?? "Server=localhost,1466;Database=IDCS;User Id=sa;Password=IdcsPassword123!;TrustServerCertificate=True;";
    }

    public async Task Consume(ConsumeContext<ManifestDeliveredIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received ManifestDelivered event for Manifest {ManifestNo} (ID: {ManifestId}). Writing back to IDCS...", 
            message.ManifestNo, message.ManifestId);

        try
        {
            using var connection = new SqlConnection(_idcsConnectionString);
            await connection.OpenAsync(context.CancellationToken);

            var sql = @"
                INSERT INTO edcl_delivery_status (ManifestId, ManifestNo, Status, DeliveredAt, Remarks)
                VALUES (@ManifestId, @ManifestNo, @Status, @DeliveredAt, @Remarks)";

            await connection.ExecuteAsync(sql, new
            {
                message.ManifestId,
                message.ManifestNo,
                message.Status,
                message.DeliveredAt,
                message.Remarks
            });

            _logger.LogInformation("Successfully wrote back delivery status for Manifest {ManifestNo} to IDCS.", message.ManifestNo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write back delivery status for Manifest {ManifestNo} to IDCS.", message.ManifestNo);
            throw; // Re-throw to allow MassTransit to handle retries/faults
        }
    }
}
