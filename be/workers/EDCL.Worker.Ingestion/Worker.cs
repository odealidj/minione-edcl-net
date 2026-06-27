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
            
            int maxRetries = 5;
            int attempt = 0;
            bool success = false;
            
            while (attempt < maxRetries && !success)
            {
                try
                {
                    await ProcessMessageAsync(message);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    success = true;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogWarning(ex, "Error processing message. Attempt {Attempt} of {MaxRetries}", attempt, maxRetries);
                    
                    if (attempt >= maxRetries)
                    {
                        _logger.LogError("Message failed after {MaxRetries} attempts. Publishing to DLQ.", maxRetries);
                        
                        string eventType = "Unknown";
                        try
                        {
                            using var doc = JsonDocument.Parse(message);
                            if (doc.RootElement.TryGetProperty("source", out var sourceProp) && 
                                sourceProp.TryGetProperty("table", out var tableProp))
                            {
                                eventType = tableProp.GetString() ?? "Unknown";
                            }
                        }
                        catch { /* Ignore parsing errors for DLQ */ }

                        var faultEvent = new
                        {
                            EventType = eventType,
                            Payload = message,
                            ErrorMessage = ex.Message,
                            StackTrace = ex.StackTrace,
                            OccurredAt = DateTime.UtcNow
                        };
                        
                        var faultJson = JsonSerializer.Serialize(faultEvent);
                        var faultBody = Encoding.UTF8.GetBytes(faultJson);
                        
                        var props = new RabbitMQ.Client.BasicProperties { ContentType = "application/json" };
                        await channel.QueueDeclareAsync("edcl_ingestion_faults", true, false, false, null, cancellationToken: stoppingToken);
                        
                        await channel.BasicPublishAsync(
                            exchange: "",
                            routingKey: "edcl_ingestion_faults",
                            mandatory: false,
                            basicProperties: props,
                            body: faultBody,
                            cancellationToken: stoppingToken);
                            
                        // Ack the original message so it doesn't infinite loop
                        await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        success = true; // Break the loop
                    }
                    else
                    {
                        // Exponential backoff: 2s, 4s, 8s, 16s...
                        var delayMs = (int)Math.Pow(2, attempt) * 1000;
                        await Task.Delay(delayMs, stoppingToken);
                    }
                }
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

        _logger.LogInformation($"Received CDC event for table: {table}");

        if (table?.Equals("Manifests", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifests", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestAsync(after);
        }
        else if (table?.Equals("ManifestParts", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifest_parts", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestPartAsync(after);
        }
        else if (table?.Equals("ManifestKanbans", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifest_kanbans", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestKanbanAsync(after);
        }
        else if (table?.Equals("ManifestSkids", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifest_skids", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestSkidAsync(after);
        }
        else
        {
            _logger.LogWarning($"No processor found for table: {table}");
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

    private async Task ProcessManifestPartAsync(JsonElement after)
    {
        _logger.LogInformation($"Processing ManifestPart Debezium payload: {after.GetRawText()}");
        
        long id = 0;
        if (after.TryGetProperty("Id", out var idProp) || after.TryGetProperty("id", out idProp))
            id = idProp.GetInt64();
            
        long manifestId = 0;
        if (after.TryGetProperty("ManifestId", out var miProp) || after.TryGetProperty("manifest_id", out miProp))
            manifestId = miProp.GetInt64();
            
        string partNo = "";
        if (after.TryGetProperty("PartNo", out var pnProp) || after.TryGetProperty("part_no", out pnProp))
            partNo = pnProp.GetString() ?? "";

        string partName = "";
        if (after.TryGetProperty("PartName", out var pnameProp) || after.TryGetProperty("part_name", out pnameProp))
            partName = pnameProp.GetString() ?? "";

        int qty = 0;
        if (after.TryGetProperty("Qty", out var qProp) || after.TryGetProperty("qty", out qProp))
            qty = qProp.GetInt32();

        string kanbanNo = "";
        if (after.TryGetProperty("KanbanNo", out var knProp) || after.TryGetProperty("kanban_no", out knProp))
            kanbanNo = knProp.GetString() ?? "";
            
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
            SET IDENTITY_INSERT edcl.ingestion.manifest_parts ON;
            
            MERGE INTO edcl.ingestion.manifest_parts AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET 
                    ManifestId = @ManifestId,
                    PartNo = @PartNo,
                    PartName = @PartName,
                    Qty = @Qty,
                    KanbanNo = @KanbanNo,
                    Status = @Status,
                    UpdatedAt = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, ManifestId, PartNo, PartName, Qty, KanbanNo, Status, CreatedAt, CreatedBy, IsDeleted, RowVersion)
                VALUES (@Id, @ManifestId, @PartNo, @PartName, @Qty, @KanbanNo, @Status, @CreatedAt, 'System', 0, CAST(0 AS varbinary(8)));
                
            SET IDENTITY_INSERT edcl.ingestion.manifest_parts OFF;";

        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            ManifestId = manifestId, 
            PartNo = partNo, 
            PartName = partName,
            Qty = qty,
            KanbanNo = kanbanNo,
            Status = status,
            CreatedAt = createdAt
        });
        
        _logger.LogInformation($"Successfully upserted ManifestPart ID {id}");
    }

    private async Task ProcessManifestKanbanAsync(JsonElement after)
    {
        _logger.LogInformation($"Processing ManifestKanban Debezium payload: {after.GetRawText()}");
        
        long id = 0;
        if (after.TryGetProperty("Id", out var idProp) || after.TryGetProperty("id", out idProp))
            id = idProp.GetInt64();
            
        long manifestId = 0;
        if (after.TryGetProperty("ManifestId", out var miProp) || after.TryGetProperty("manifest_id", out miProp))
            manifestId = miProp.GetInt64();
            
        string partNo = "";
        if (after.TryGetProperty("PartNo", out var pnProp) || after.TryGetProperty("part_no", out pnProp))
            partNo = pnProp.GetString() ?? "";

        string kanbanCd = "";
        if (after.TryGetProperty("KanbanCd", out var kcdProp) || after.TryGetProperty("kanban_cd", out kcdProp))
            kanbanCd = kcdProp.GetString() ?? "";
        
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
            SET IDENTITY_INSERT edcl.ingestion.manifest_kanbans ON;
            
            MERGE INTO edcl.ingestion.manifest_kanbans AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET 
                    ManifestId = @ManifestId,
                    PartNo = @PartNo,
                    KanbanCd = @KanbanCd,
                    UpdatedAt = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, ManifestId, PartNo, KanbanCd, CreatedAt, CreatedBy, IsDeleted, RowVersion)
                VALUES (@Id, @ManifestId, @PartNo, @KanbanCd, @CreatedAt, 'System', 0, CAST(0 AS varbinary(8)));
                
            SET IDENTITY_INSERT edcl.ingestion.manifest_kanbans OFF;";

        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            ManifestId = manifestId, 
            PartNo = partNo, 
            KanbanCd = kanbanCd,
            CreatedAt = createdAt
        });
        
        _logger.LogInformation($"Successfully upserted ManifestKanban ID {id}");
    }

    private async Task ProcessManifestSkidAsync(JsonElement after)
    {
        _logger.LogInformation($"Processing ManifestSkid Debezium payload: {after.GetRawText()}");
        
        long id = 0;
        if (after.TryGetProperty("Id", out var idProp) || after.TryGetProperty("id", out idProp))
            id = idProp.GetInt64();
            
        long manifestId = 0;
        if (after.TryGetProperty("ManifestId", out var miProp) || after.TryGetProperty("manifest_id", out miProp))
            manifestId = miProp.GetInt64();
            
        string skidNo = "";
        if (after.TryGetProperty("SkidNo", out var skProp) || after.TryGetProperty("skid_no", out skProp))
            skidNo = skProp.GetString() ?? "";
        
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
            SET IDENTITY_INSERT edcl.ingestion.manifest_skids ON;
            
            MERGE INTO edcl.ingestion.manifest_skids AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET 
                    ManifestId = @ManifestId,
                    SkidNo = @SkidNo,
                    UpdatedAt = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, ManifestId, SkidNo, CreatedAt, CreatedBy, IsDeleted, RowVersion)
                VALUES (@Id, @ManifestId, @SkidNo, @CreatedAt, 'System', 0, CAST(0 AS varbinary(8)));
                
            SET IDENTITY_INSERT edcl.ingestion.manifest_skids OFF;";

        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            ManifestId = manifestId, 
            SkidNo = skidNo, 
            CreatedAt = createdAt
        });
        
        _logger.LogInformation($"Successfully upserted ManifestSkid ID {id}");
    }
}
