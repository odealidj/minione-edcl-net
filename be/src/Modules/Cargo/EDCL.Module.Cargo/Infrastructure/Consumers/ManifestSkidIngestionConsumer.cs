using Dapper;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class ManifestSkidIngestionConsumer : IConsumer<DebeziumManifestSkidEvent>
{
    private readonly string _connectionString;
    private readonly ILogger<ManifestSkidIngestionConsumer> _logger;

    public ManifestSkidIngestionConsumer(IConfiguration configuration, ILogger<ManifestSkidIngestionConsumer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new InvalidOperationException("DefaultConnection not found.");
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DebeziumManifestSkidEvent> context)
    {
        var payload = context.Message.Payload;
        if (payload == null) return;

        var op = payload.Op;
        
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(context.CancellationToken);

        switch (op)
        {
            case "c":
            case "r":
            case "u":
                if (payload.After == null) return;
                await HandleUpsertAsync(connection, payload.After);
                break;
            case "d":
                if (payload.Before == null) return;
                await HandleDeleteAsync(connection, payload.Before.Id);
                break;
            default:
                _logger.LogWarning("Unknown Debezium operation '{Op}' for ManifestSkid ID {Id}", op, payload.After?.Id ?? payload.Before?.Id);
                break;
        }
    }

    private async Task HandleUpsertAsync(SqlConnection connection, ManifestSkidDto data)
    {
        const string sql = @"
            SET IDENTITY_INSERT [ingestion].[manifest_skids] ON;

            MERGE INTO [ingestion].[manifest_skids] AS Target
            USING (VALUES (@Id, @ManifestId, @SkidNo))
               AS Source (Id, ManifestId, SkidNo)
            ON Target.Id = Source.Id
            WHEN MATCHED THEN
               UPDATE SET 
                   ManifestId = Source.ManifestId,
                   SkidNo = Source.SkidNo
            WHEN NOT MATCHED THEN
               INSERT (Id, ManifestId, SkidNo)
               VALUES (Source.Id, Source.ManifestId, Source.SkidNo);

            SET IDENTITY_INSERT [ingestion].[manifest_skids] OFF;
        ";

        await connection.ExecuteAsync(sql, new
        {
            data.Id,
            data.ManifestId,
            data.SkidNo
        });
        
        _logger.LogInformation("Upserted ManifestSkid {Id} for Manifest {ManifestId}", data.Id, data.ManifestId);
    }

    private async Task HandleDeleteAsync(SqlConnection connection, long id)
    {
        const string sql = "DELETE FROM [ingestion].[manifest_skids] WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        if (rowsAffected > 0)
        {
            _logger.LogInformation("Deleted ManifestSkid {Id}", id);
        }
    }
}
