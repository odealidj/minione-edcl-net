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

    // --- Metrics State ---
    private int _sessionSuccessCount = 0;
    private int _sessionFailedCount = 0;
    private int _sessionProcessedCount = 0;
    private long? _currentSessionId = null;
    private DateTime _sessionStartTime = DateTime.UtcNow;
    private DateTime _lastMessageTime = DateTime.UtcNow;
    private bool _sessionMetricsDirty = false;
    private Dictionary<string, int> _sessionEventBreakdown = new();
    private readonly object _metricsLock = new object();
    // Reset mode: setelah external reset (delete sync_sessions), blokir pembuatan session baru
    // selama 120 detik agar CDC events dari proses reset tidak membuat ghost session.
    private DateTime _resetModeExpiresAt = DateTime.MinValue;
    // ---------------------

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

        await channel.BasicQosAsync(0, 100, false);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            int maxRetries = 5;
            int attempt = 1;

            if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryObj))
            {
                if (retryObj is int r) attempt = r;
                else if (retryObj is byte[] rb && rb.Length == 4) attempt = BitConverter.ToInt32(rb);
                else if (int.TryParse(retryObj!.ToString(), out int r2)) attempt = r2;
            }
            
            string eventType = "Unknown";
            string opType = "u";
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                if (!root.TryGetProperty("payload", out var payload)) payload = root;

                if (payload.TryGetProperty("source", out var sourceProp) && 
                    sourceProp.TryGetProperty("table", out var tableProp))
                {
                    eventType = tableProp.GetString() ?? "Unknown";
                }
                if (payload.TryGetProperty("op", out var opProp))
                {
                    opType = opProp.GetString() ?? "u";
                }
            }
            catch { /* Ignore parsing errors */ }
            
            string breakdownKey = $"{eventType.ToLower()}_{opType.ToLower()}";

            // Hanya buat session baru untuk INSERT/UPDATE events.
            // DELETE events (op == "d") setelah reset tidak boleh memicu session baru —
            // ini adalah "cleanup artifacts" dari proses make reset-master-one.
            bool hasActiveSession;
            lock (_metricsLock) { hasActiveSession = _currentSessionId.HasValue; }
            
            if (opType != "d" || hasActiveSession)
            {
                await EnsureSessionExistsAsync();
            }
            else
            {
                // DELETE tanpa sesi aktif: log dan skip (tidak perlu buat sesi baru)
                _logger.LogDebug("Skipping session creation for DELETE event on table '{Table}' (no active session — likely post-reset cleanup).", eventType);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                return;
            }

            try
            {
                await ProcessMessageAsync(message);
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                
                lock (_metricsLock)
                {
                    _sessionSuccessCount++;
                    _sessionProcessedCount++;
                    _lastMessageTime = DateTime.UtcNow;
                    _sessionMetricsDirty = true;
                    
                    if (!_sessionEventBreakdown.ContainsKey(breakdownKey))
                        _sessionEventBreakdown[breakdownKey] = 0;
                    _sessionEventBreakdown[breakdownKey]++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing message. Attempt {Attempt} of {MaxRetries}", attempt, maxRetries);
                
                if (attempt >= maxRetries)
                {
                    _logger.LogError("Message failed after {MaxRetries} attempts. Publishing to DLQ.", maxRetries);
                    
                    // eventType is already parsed above
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
                        
                    lock (_metricsLock)
                    {
                        _sessionFailedCount++;
                        _sessionProcessedCount++;
                        _lastMessageTime = DateTime.UtcNow;
                        _sessionMetricsDirty = true;
                        
                        string errKey = $"{breakdownKey}_err";
                        if (!_sessionEventBreakdown.ContainsKey(errKey))
                            _sessionEventBreakdown[errKey] = 0;
                        _sessionEventBreakdown[errKey]++;
                    }
                }
                else
                {
                    var delayMs = (int)Math.Pow(2, attempt) * 1000;
                    var retryQueueName = $"edcl_ingestion_retry_{delayMs}";

                    var queueArgs = new Dictionary<string, object?>
                    {
                        { "x-dead-letter-exchange", "" },
                        { "x-dead-letter-routing-key", "edcl_ingestion" },
                        { "x-message-ttl", delayMs }
                    };

                    await channel.QueueDeclareAsync(retryQueueName, true, false, false, queueArgs, cancellationToken: stoppingToken);

                    var headers = ea.BasicProperties.Headers != null 
                        ? new Dictionary<string, object?>(ea.BasicProperties.Headers!) 
                        : new Dictionary<string, object?>();
                        
                    headers["x-retry-count"] = attempt + 1;
                    
                    var retryProps = new RabbitMQ.Client.BasicProperties
                    {
                        Headers = headers,
                        ContentType = ea.BasicProperties.ContentType
                    };

                    await channel.BasicPublishAsync(
                        exchange: "",
                        routingKey: retryQueueName,
                        mandatory: false,
                        basicProperties: retryProps,
                        body: body,
                        cancellationToken: stoppingToken);
                }
                
                // Ack the original message so it unblocks the queue
                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(queue: "edcl_ingestion", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2000, stoppingToken);
            
            bool shouldUpdateDb = false;
            long? sessionIdToUpdate = null;
            bool shouldCompleteSession = false;
            int currentSuccess = 0;
            int currentFailed = 0;
            int currentProcessed = 0;
            string currentBreakdownJson = "{}";

            lock (_metricsLock)
            {
                if (_currentSessionId.HasValue)
                {
                    if ((DateTime.UtcNow - _lastMessageTime).TotalHours >= 1)
                    {
                        shouldCompleteSession = true;
                        sessionIdToUpdate = _currentSessionId;
                        _currentSessionId = null;
                    }
                    else if (_sessionMetricsDirty)
                    {
                        shouldUpdateDb = true;
                        sessionIdToUpdate = _currentSessionId;
                        _sessionMetricsDirty = false;
                    }
                    currentSuccess = _sessionSuccessCount;
                    currentFailed = _sessionFailedCount;
                    currentProcessed = _sessionProcessedCount;
                    currentBreakdownJson = JsonSerializer.Serialize(_sessionEventBreakdown);
                }
            }

            if (shouldCompleteSession && sessionIdToUpdate.HasValue)
            {
                await CompleteSessionAsync(sessionIdToUpdate.Value, currentProcessed, currentSuccess, currentFailed, currentBreakdownJson);
            }
            else if (shouldUpdateDb && sessionIdToUpdate.HasValue)
            {
                await UpdateSessionMetricsAsync(sessionIdToUpdate.Value, currentProcessed, currentSuccess, currentFailed, currentBreakdownJson);
                await PublishMetricsEventAsync(channel, sessionIdToUpdate.Value, currentProcessed, currentSuccess, currentFailed, currentBreakdownJson, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        if (message == "default") return;

        _logger.LogInformation($"RAW MESSAGE: {message}");
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
        
        // We only care about creates, updates, deletes, and initial reads
        if (op != "c" && op != "u" && op != "d" && op != "r")
            return;
            
        JsonElement payloadData;
        if (op == "d")
        {
            if (!payload.TryGetProperty("before", out payloadData))
                return;
        }
        else
        {
            if (!payload.TryGetProperty("after", out payloadData))
                return;
        }

        _logger.LogInformation($"Received CDC event for table: {table}, op: {op}");

        if (table?.Equals("Manifests", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifests", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestAsync(payloadData, op);
        }
        else if (table?.Equals("ManifestParts", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifest_parts", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestPartAsync(payloadData, op);
        }
        else if (table?.Equals("ManifestKanbans", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifest_kanbans", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestKanbanAsync(payloadData, op);
        }
        else if (table?.Equals("ManifestSkids", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("manifest_skids", StringComparison.OrdinalIgnoreCase) == true)
        {
            await ProcessManifestSkidAsync(payloadData, op);
        }
        else if (table?.Equals("Suppliers", StringComparison.OrdinalIgnoreCase) == true
            || table?.Equals("suppliers", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (op != "d")
                await ProcessSupplierAsync(payloadData);
        }
        else
        {
            _logger.LogWarning($"No processor found for table: {table}");
        }
    }

    private string GetOperationName(string op) => op switch
    {
        "c" => "INSERT",
        "u" => "UPDATE",
        "d" => "DELETE",
        _ => "UNKNOWN"
    };

    private async Task<(bool IsLocked, string Status, string DeliveryNo, long? PickupOrderId)> IsManifestLockedAsync(SqlConnection connection, string manifestNo)
    {
        var sql = @"
            SELECT TOP 1 po.Status, po.delivery_no, po.Id 
            FROM edcl.job.pickup_orders po
            INNER JOIN edcl.job.pickup_order_details pod ON po.Id = pod.PickupOrderId
            INNER JOIN edcl.job.pickup_order_manifests pom ON pod.Id = pom.PickupOrderDetailId
            WHERE pom.ManifestNo = @ManifestNo AND po.IsDeleted = 0 AND pom.IsDeleted = 0 AND pod.IsDeleted = 0";
            
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { ManifestNo = manifestNo });
        if (result != null)
        {
            string status = result.Status;
            if (status == "ON_PROGRESS" || status == "COMPLETED")
            {
                return (true, status, result.delivery_no, result.Id);
            }
        }
        return (false, string.Empty, string.Empty, null);
    }

    private async Task LogManifestProblemAsync(SqlConnection connection, string opType, string payload, string description, string manifestNo, string deliveryNo, long? pickupOrderId)
    {
        var sql = @"
            INSERT INTO edcl.ingestion.manifest_problems (OperationType, Payload, Description, Status, OccurredAt, ManifestNo, CreatedAt, CreatedBy, DeliveryNo, PickupOrderId, IsDeleted, RowVersion)
            VALUES (@OperationType, @Payload, @Description, 'UNRESOLVED', GETUTCDATE(), @ManifestNo, GETUTCDATE(), 'System', @DeliveryNo, @PickupOrderId, 0, CAST(0 AS varbinary(8)));";

        await connection.ExecuteAsync(sql, new 
        { 
            OperationType = opType,
            Payload = payload,
            Description = description,
            ManifestNo = manifestNo,
            DeliveryNo = deliveryNo ?? (object)DBNull.Value,
            PickupOrderId = pickupOrderId ?? (object)DBNull.Value
        });
    }

    private async Task ProcessManifestAsync(JsonElement payloadData, string op)
    {
        var after = payloadData;
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

        string supplierPlant = "";
        if (after.TryGetProperty("SupplierPlant", out var spProp) || after.TryGetProperty("supplier_plant", out spProp))
            supplierPlant = spProp.GetString() ?? "";

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

        var lockCheck = await IsManifestLockedAsync(connection, manifestNo);
        if (lockCheck.IsLocked)
        {
            var opType = GetOperationName(op);
            string reason = lockCheck.Status == "ON_PROGRESS" ? "InTransit" : "Delivered";
            await LogManifestProblemAsync(connection, opType, after.GetRawText(), $"Late CDC {opType}: Manifest is already {reason}, but writing to ODS anyway.", manifestNo, lockCheck.DeliveryNo, lockCheck.PickupOrderId);
            _logger.LogWarning($"Late CDC {opType} for Manifest {manifestNo} because it is {reason}. Writing to ODS anyway.");
        }

        if (op == "d")
        {
            var sqlDelete = "DELETE FROM edcl.ingestion.Manifests WHERE ManifestNo = @ManifestNo";
            await connection.ExecuteAsync(sqlDelete, new { ManifestNo = manifestNo });
            return;
        }

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
                    supplier_plant = @SupplierPlant,
                    Sequence = @Sequence,
                    order_type = @OrderType,
                    PickDate = @PickDate,
                    cycle = @Cycle,
                    status = @Status,
                    IsAssignedToRoute = @IsAssignedToRoute,
                    UpdatedAt = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, ManifestNo, SupplierCode, SupplierName, supplier_plant, Sequence, order_type, PickDate, cycle, status, IsAssignedToRoute, CreatedAt, CreatedBy, IsDeleted, RowVersion)
                VALUES (@Id, @ManifestNo, @SupplierCode, @SupplierName, @SupplierPlant, @Sequence, @OrderType, @PickDate, @Cycle, @Status, @IsAssignedToRoute, @CreatedAt, 'System', 0, CAST(0 AS varbinary(8)));
                
            SET IDENTITY_INSERT edcl.ingestion.Manifests OFF;";

        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            ManifestNo = manifestNo, 
            SupplierCode = supplierCode, 
            SupplierName = supplierName,
            SupplierPlant = supplierPlant,
            Sequence = sequence,
            OrderType = orderType,
            PickDate = pickDate,
            Cycle = cycle,
            Status = status,
            IsAssignedToRoute = lockCheck.IsLocked,
            CreatedAt = createdAt
        });
        
        _logger.LogInformation($"Successfully upserted Manifest ID {id} into edcl.ingestion.Manifests");
    }

    private async Task ProcessManifestPartAsync(JsonElement payloadData, string op)
    {
        var after = payloadData;
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

        if (op == "d")
        {
            var sqlDelete = "DELETE FROM edcl.ingestion.manifest_parts WHERE Id = @Id";
            await connection.ExecuteAsync(sqlDelete, new { Id = id });
            return;
        }

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
                    kanban_no = @KanbanNo,
                    status = @Status,
                    UpdatedAt = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, ManifestId, PartNo, PartName, Qty, kanban_no, status, CreatedAt, CreatedBy, IsDeleted, RowVersion)
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

    private async Task ProcessManifestKanbanAsync(JsonElement payloadData, string op)
    {
        var after = payloadData;
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

        if (op == "d")
        {
            var sqlDelete = "DELETE FROM edcl.ingestion.manifest_kanbans WHERE Id = @Id";
            await connection.ExecuteAsync(sqlDelete, new { Id = id });
            return;
        }

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

    private async Task ProcessManifestSkidAsync(JsonElement payloadData, string op)
    {
        var after = payloadData;
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

        if (op == "d")
        {
            var sqlDelete = "DELETE FROM edcl.ingestion.manifest_skids WHERE Id = @Id";
            await connection.ExecuteAsync(sqlDelete, new { Id = id });
            return;
        }

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

    private async Task ProcessSupplierAsync(JsonElement after)
    {
        _logger.LogInformation($"Processing Supplier Debezium payload: {after.GetRawText()}");
        
        long id = 0;
        if (after.TryGetProperty("Id", out var idProp) || after.TryGetProperty("id", out idProp))
            id = idProp.GetInt64();
            
        string supplierCode = "";
        if (after.TryGetProperty("SupplierCode", out var scProp) || after.TryGetProperty("supplier_code", out scProp))
            supplierCode = scProp.GetString() ?? "";

        string supplierName = "";
        if (after.TryGetProperty("SupplierName", out var snProp) || after.TryGetProperty("supplier_name", out snProp))
            supplierName = snProp.GetString() ?? "";

        string address = "";
        if (after.TryGetProperty("Address", out var addrProp) || after.TryGetProperty("address", out addrProp))
            address = addrProp.GetString() ?? "";

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            MERGE INTO edcl.driver.suppliers AS target
            USING (SELECT @SupplierCode AS SupplierCode) AS source
            ON target.SupplierCode = source.SupplierCode
            WHEN MATCHED THEN
                UPDATE SET 
                    Name = @SupplierName,
                    Address = @Address,
                    updated_at = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (SupplierCode, Name, Address, created_at, created_by, is_deleted)
                VALUES (@SupplierCode, @SupplierName, @Address, GETUTCDATE(), 'System', 0);";

        await connection.ExecuteAsync(sql, new 
        { 
            Id = id, 
            SupplierCode = supplierCode, 
            SupplierName = supplierName,
            Address = address
        });
        
        _logger.LogInformation($"Successfully upserted Supplier ID {id} into edcl.driver.suppliers");
    }

    private async Task EnsureSessionExistsAsync()
    {
        if (_currentSessionId.HasValue) return;

        // Jika sedang dalam reset mode, jangan buat session baru.
        // Ini mencegah CDC events dari proses reset memicu ghost session.
        if (DateTime.UtcNow < _resetModeExpiresAt)
        {
            _logger.LogDebug("EnsureSessionExistsAsync: skipped (reset mode active until {Until})", _resetModeExpiresAt);
            return;
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Cleanup sessions older than 30 days
        var cleanupSql = "DELETE FROM edcl.ingestion.sync_sessions WHERE SessionDate < DATEADD(day, -30, GETUTCDATE())";
        await connection.ExecuteAsync(cleanupSql);

        var sql = @"
            INSERT INTO edcl.ingestion.sync_sessions 
            (SessionDate, StartTime, TotalProcessed, SuccessCount, FailedCount, Status, CreatedAt, CreatedBy, IsDeleted, RowVersion)
            OUTPUT INSERTED.Id
            VALUES (CAST(GETUTCDATE() AS DATE), GETUTCDATE(), 0, 0, 0, 'IN_PROGRESS', GETUTCDATE(), 'System', 0, CAST(0 AS varbinary(8)));";

        var id = await connection.ExecuteScalarAsync<long>(sql);
        
        lock (_metricsLock)
        {
            _currentSessionId = id;
            _sessionStartTime = DateTime.UtcNow;
            _sessionSuccessCount = 0;
            _sessionFailedCount = 0;
            _sessionProcessedCount = 0;
            _sessionEventBreakdown.Clear();
            _sessionMetricsDirty = false;
        }
        
        _logger.LogInformation($"Created new SyncSession ID {id}");
    }

    private async Task CompleteSessionAsync(long sessionId, int processed, int success, int failed, string breakdownJson)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            UPDATE edcl.ingestion.sync_sessions 
            SET EndTime = GETUTCDATE(),
                TotalProcessed = @Processed,
                SuccessCount = @Success,
                FailedCount = @Failed,
                EventBreakdown = @Breakdown,
                Status = 'COMPLETED',
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id;";

        await connection.ExecuteAsync(sql, new { Id = sessionId, Processed = processed, Success = success, Failed = failed, Breakdown = breakdownJson });
        _logger.LogInformation($"Completed SyncSession ID {sessionId}");
    }

    private async Task UpdateSessionMetricsAsync(long sessionId, int processed, int success, int failed, string breakdownJson)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            UPDATE edcl.ingestion.sync_sessions 
            SET TotalProcessed = @Processed,
                SuccessCount = @Success,
                FailedCount = @Failed,
                EventBreakdown = @Breakdown,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id;";

        var affected = await connection.ExecuteAsync(sql, new { Id = sessionId, Processed = processed, Success = success, Failed = failed, Breakdown = breakdownJson });
        if (affected == 0)
        {
            // Session tidak ditemukan di DB — kemungkinan besar sudah dihapus oleh proses reset.
            // Aktifkan reset mode selama 120 detik: blokir pembuatan session baru agar CDC
            // events dari proses reset (delete manifests, suppliers, dll.) tidak membuat ghost session.
            _logger.LogWarning($"SyncSession {sessionId} not found in DB. Activating reset mode for 120 seconds.");
            _resetModeExpiresAt = DateTime.UtcNow.AddSeconds(120);
            lock (_metricsLock)
            {
                if (_currentSessionId == sessionId)
                {
                    _currentSessionId = null;
                    _sessionSuccessCount = 0;
                    _sessionFailedCount = 0;
                    _sessionProcessedCount = 0;
                    _sessionEventBreakdown.Clear();
                    _sessionMetricsDirty = false;
                }
            }
        }
    }

    private async Task PublishMetricsEventAsync(IChannel channel, long sessionId, int processed, int success, int failed, string breakdownJson, CancellationToken ct)
    {
        var metricsEvent = new
        {
            SessionId = sessionId,
            SessionDate = DateTime.UtcNow.Date,
            StartTime = _sessionStartTime,
            EndTime = (DateTime?)null,
            TotalProcessed = processed,
            SuccessCount = success,
            FailedCount = failed,
            EventBreakdown = breakdownJson,
            Status = "IN_PROGRESS"
        };

        var json = JsonSerializer.Serialize(metricsEvent);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new RabbitMQ.Client.BasicProperties { ContentType = "application/json" };
        
        // Ensure exchange exists (MassTransit will bind queue to this)
        await channel.ExchangeDeclareAsync("EDCL.Module.Cargo.Domain.Events:IngestionMetricsEvent", "fanout", true, false, null, cancellationToken: ct);
        
        await channel.BasicPublishAsync(
            exchange: "EDCL.Module.Cargo.Domain.Events:IngestionMetricsEvent",
            routingKey: "",
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);
    }
}
