using Dapper;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class ManifestPartIngestionConsumer : IConsumer<DebeziumManifestPartEvent>
{
    private readonly string _connectionString;
    private readonly ILogger<ManifestPartIngestionConsumer> _logger;

    public ManifestPartIngestionConsumer(IConfiguration configuration, ILogger<ManifestPartIngestionConsumer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new InvalidOperationException("DefaultConnection not found.");
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DebeziumManifestPartEvent> context)
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
                _logger.LogWarning("Unknown Debezium operation '{Op}' for ManifestPart ID {Id}", op, payload.After?.Id ?? payload.Before?.Id);
                break;
        }
    }

    private async Task HandleUpsertAsync(SqlConnection connection, ManifestPartDto data)
    {
        const string sql = @"
            SET IDENTITY_INSERT [ingestion].[manifest_parts] ON;

            MERGE INTO [ingestion].[manifest_parts] AS Target
            USING (VALUES (@Id, @ManifestId, @PartNo, @PartName, @KanbanNo, @Status))
               AS Source (Id, ManifestId, PartNo, PartName, KanbanNo, Status)
            ON Target.Id = Source.Id
            WHEN MATCHED THEN
               UPDATE SET 
                   ManifestId = Source.ManifestId,
                   PartNo = Source.PartNo,
                   PartName = Source.PartName,
                   KanbanNo = Source.KanbanNo,
                   Status = Source.Status
            WHEN NOT MATCHED THEN
               INSERT (Id, ManifestId, PartNo, PartName, KanbanNo, Status)
               VALUES (Source.Id, Source.ManifestId, Source.PartNo, Source.PartName, Source.KanbanNo, Source.Status);

            SET IDENTITY_INSERT [ingestion].[manifest_parts] OFF;
        ";

        await connection.ExecuteAsync(sql, new
        {
            data.Id,
            data.ManifestId,
            data.PartNo,
            data.PartName,
            data.KanbanNo,
            data.Status
        });
        
        _logger.LogInformation("Upserted ManifestPart {Id} for Manifest {ManifestId}", data.Id, data.ManifestId);
    }

    private async Task HandleDeleteAsync(SqlConnection connection, long id)
    {
        const string sql = "DELETE FROM [ingestion].[manifest_parts] WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        if (rowsAffected > 0)
        {
            _logger.LogInformation("Deleted ManifestPart {Id}", id);
        }
    }
}
