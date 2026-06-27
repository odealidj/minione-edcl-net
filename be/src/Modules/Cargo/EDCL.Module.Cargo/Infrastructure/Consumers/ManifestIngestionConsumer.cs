using Dapper;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class ManifestIngestionConsumer : IConsumer<DebeziumManifestEvent>
{
    private readonly string _connectionString;
    private readonly ILogger<ManifestIngestionConsumer> _logger;

    public ManifestIngestionConsumer(IConfiguration configuration, ILogger<ManifestIngestionConsumer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new InvalidOperationException("DefaultConnection not found.");
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DebeziumManifestEvent> context)
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
                _logger.LogWarning("Unknown Debezium operation '{Op}' for Manifest ID {Id}", op, payload.After?.Id ?? payload.Before?.Id);
                break;
        }
    }

    private async Task HandleUpsertAsync(SqlConnection connection, ManifestDto data)
    {
        // Using SET IDENTITY_INSERT ON allows us to sync the exact ID from IDCS
        const string sql = @"
            SET IDENTITY_INSERT [ingestion].[manifests] ON;

            MERGE INTO [ingestion].[manifests] AS Target
            USING (VALUES (@Id, @ManifestNo, @SupplierCode, @SupplierName, @Sequence, @OrderType, @PickDate, @Cycle, @Status))
               AS Source (Id, ManifestNo, SupplierCode, SupplierName, Sequence, OrderType, PickDate, Cycle, Status)
            ON Target.Id = Source.Id
            WHEN MATCHED THEN
               UPDATE SET 
                   ManifestNo = Source.ManifestNo,
                   SupplierCode = Source.SupplierCode,
                   SupplierName = Source.SupplierName,
                   Sequence = Source.Sequence,
                   OrderType = Source.OrderType,
                   PickDate = Source.PickDate,
                   Cycle = Source.Cycle,
                   Status = Source.Status,
                   LastModifiedAt = GETUTCDATE()
            WHEN NOT MATCHED THEN
               INSERT (Id, ManifestNo, SupplierCode, SupplierName, Sequence, OrderType, PickDate, Cycle, Status, CreatedAt)
               VALUES (Source.Id, Source.ManifestNo, Source.SupplierCode, Source.SupplierName, Source.Sequence, Source.OrderType, Source.PickDate, Source.Cycle, Source.Status, GETUTCDATE());

            SET IDENTITY_INSERT [ingestion].[manifests] OFF;
        ";

        await connection.ExecuteAsync(sql, new
        {
            data.Id,
            data.ManifestNo,
            data.SupplierCode,
            data.SupplierName,
            data.Sequence,
            data.OrderType,
            data.PickDate,
            data.Cycle,
            data.Status
        });
        
        _logger.LogInformation("Upserted Manifest {Id} - {ManifestNo}", data.Id, data.ManifestNo);
    }

    private async Task HandleDeleteAsync(SqlConnection connection, long id)
    {
        const string sql = "DELETE FROM [ingestion].[manifests] WHERE Id = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        if (rowsAffected > 0)
        {
            _logger.LogInformation("Deleted Manifest {Id}", id);
        }
    }
}
