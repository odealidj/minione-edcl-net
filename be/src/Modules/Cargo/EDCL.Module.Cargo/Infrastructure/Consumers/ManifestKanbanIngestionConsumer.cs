using Dapper;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class ManifestKanbanIngestionConsumer : IConsumer<DebeziumManifestKanbanEvent>
{
    private readonly string _connectionString;
    private readonly ILogger<ManifestKanbanIngestionConsumer> _logger;

    public ManifestKanbanIngestionConsumer(IConfiguration configuration, ILogger<ManifestKanbanIngestionConsumer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new InvalidOperationException("DefaultConnection not found.");
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DebeziumManifestKanbanEvent> context)
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
                _logger.LogWarning("Unknown Debezium operation '{Op}' for ManifestKanban ID {Id}", op, payload.After?.Id ?? payload.Before?.Id);
                break;
        }
    }

    private async Task HandleUpsertAsync(SqlConnection connection, ManifestKanbanDto data)
    {
        const string sql = @"
            SET IDENTITY_INSERT [ingestion].[manifest_kanbans] ON;

            MERGE INTO [ingestion].[manifest_kanbans] AS Target
            USING (VALUES (@Id, @ManifestId, @PartNo, @KanbanCd))
               AS Source (Id, ManifestId, PartNo, KanbanCd)
            ON Target.Id = Source.Id
            WHEN MATCHED THEN
               UPDATE SET 
                   ManifestId = Source.ManifestId,
                   PartNo = Source.PartNo,
                   KanbanCd = Source.KanbanCd
            WHEN NOT MATCHED THEN
               INSERT (Id, ManifestId, PartNo, KanbanCd)
               VALUES (Source.Id, Source.ManifestId, Source.PartNo, Source.KanbanCd);

            SET IDENTITY_INSERT [ingestion].[manifest_kanbans] OFF;
        ";

        await connection.ExecuteAsync(sql, new
        {
            data.Id,
            data.ManifestId,
            data.PartNo,
            data.KanbanCd
        });
        
        _logger.LogInformation("Upserted ManifestKanban {Id} for Manifest {ManifestId}", data.Id, data.ManifestId);
    }

    private async Task HandleDeleteAsync(SqlConnection connection, long id)
    {
        const string sql = "DELETE FROM [ingestion].[manifest_kanbans] WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        if (rowsAffected > 0)
        {
            _logger.LogInformation("Deleted ManifestKanban {Id}", id);
        }
    }
}
