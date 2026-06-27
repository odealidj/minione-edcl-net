using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Dapper;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EDCL.Worker.Ingestion;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly string _connectionString;
    private readonly string _rabbitMqConnectionString;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("DefaultConnection");
        _rabbitMqConnectionString = configuration.GetConnectionString("RabbitMqConnection") 
            ?? throw new ArgumentNullException("RabbitMqConnection");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ingestion Worker started. Connecting to RabbitMQ...");

        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMqConnectionString) };
        var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Ensure exchange and queue exist
        await channel.ExchangeDeclareAsync("debezium_events", "topic", true, false, null, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync("edcl_ingestion", true, false, false, null, cancellationToken: stoppingToken);
        await channel.QueueBindAsync("edcl_ingestion", "debezium_events", "#", null, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                await ProcessMessageAsync(message);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue: "edcl_ingestion", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        using var document = JsonDocument.Parse(message);
        var root = document.RootElement;
        
        // Check if this is a Debezium payload
        if (!root.TryGetProperty("payload", out var payload))
        {
            // Try to use root directly if there is no payload wrapper (depends on debezium configuration)
            payload = root;
        }

        if (!payload.TryGetProperty("source", out var source) || !source.TryGetProperty("table", out var tableElement))
            return;

        var table = tableElement.GetString();
        
        if (!payload.TryGetProperty("op", out var opElement))
            return;
            
        var op = opElement.GetString();
        
        // We only care about creates and updates
        if (op != "c" && op != "u")
            return;
            
        if (!payload.TryGetProperty("after", out var after))
            return;

        if (table?.Equals("Manifests", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestAsync(after);
        }
    }

    private async Task ProcessManifestAsync(JsonElement after)
    {
        _logger.LogInformation($"Processing Debezium payload: {after.GetRawText()}");
        
        long id = 0;
        if (after.TryGetProperty("Id", out var idProp) || after.TryGetProperty("id", out idProp))
            id = idProp.GetInt64();
            
        string manifestNo = "";
        if (after.TryGetProperty("ManifestNo", out var mnProp) || after.TryGetProperty("manifest_no", out mnProp))
            manifestNo = mnProp.GetString() ?? "";
            
        string supplierCode = "";
        if (after.TryGetProperty("SupplierCode", out var scProp) || after.TryGetProperty("supplier_code", out scProp))
            supplierCode = scProp.GetString() ?? "";

        string supplierName = "";
        if (after.TryGetProperty("SupplierName", out var snProp) || after.TryGetProperty("supplier_name", out snProp))
            supplierName = snProp.GetString() ?? "";

        int sequence = 0;
        if (after.TryGetProperty("Sequence", out var seqProp) || after.TryGetProperty("sequence", out seqProp))
            sequence = seqProp.GetInt32();

        string orderType = "";
        if (after.TryGetProperty("OrderType", out var otProp) || after.TryGetProperty("order_type", out otProp))
            orderType = otProp.GetString() ?? "";

        DateTime pickDate = DateTime.UtcNow;
        if (after.TryGetProperty("PickDate", out var pdProp) || after.TryGetProperty("pick_date", out pdProp))
        {
            if (pdProp.ValueKind == JsonValueKind.Number)
            {
                var pickDateMs = pdProp.GetInt64();
                pickDate = DateTimeOffset.FromUnixTimeMilliseconds(pickDateMs).DateTime; // Note: PickDate is probably just milliseconds
            }
        }

        string cycle = "";
        if (after.TryGetProperty("Cycle", out var cyProp) || after.TryGetProperty("cycle", out cyProp))
            cycle = cyProp.GetString() ?? "";
            
        string status = "";
        if (after.TryGetProperty("Status", out var stProp) || after.TryGetProperty("status", out stProp))
            status = stProp.GetString() ?? "";
        
        DateTime createdAt = DateTime.UtcNow;
        if (after.TryGetProperty("CreatedAt", out var caProp) || after.TryGetProperty("created_at", out caProp))
        {
            if (caProp.ValueKind == JsonValueKind.Number)
            {
                var createdAtMs = caProp.GetInt64();
                createdAt = DateTimeOffset.FromUnixTimeMilliseconds(createdAtMs / 1000).DateTime; // assuming microseconds
            }
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SET IDENTITY_INSERT edcl.ingestion.Manifests ON;
            
            MERGE INTO edcl.ingestion.Manifests AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET 
                    ManifestNo = @ManifestNo,
                    SupplierCode = @SupplierCode,
                    SupplierName = @SupplierName,
                    Sequence = @Sequence,
                    OrderType = @OrderType,
                    PickDate = @PickDate,
                    Cycle = @Cycle,
                    Status = @Status,
                    UpdatedAt = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, ManifestNo, SupplierCode, SupplierName, Sequence, OrderType, PickDate, Cycle, Status, CreatedAt, CreatedBy, IsDeleted, RowVersion)
                VALUES (@Id, @ManifestNo, @SupplierCode, @SupplierName, @Sequence, @OrderType, @PickDate, @Cycle, @Status, @CreatedAt, 'System', 0, CAST(0 AS varbinary(8)));
                
            SET IDENTITY_INSERT edcl.ingestion.Manifests OFF;";

        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            ManifestNo = manifestNo, 
            SupplierCode = supplierCode, 
            SupplierName = supplierName,
            Sequence = sequence,
            OrderType = orderType,
            PickDate = pickDate,
            Cycle = cycle,
            Status = status,
            CreatedAt = createdAt
        });
        
        _logger.LogInformation($"Successfully upserted Manifest ID {id} into edcl.ingestion.Manifests");
    }
}
