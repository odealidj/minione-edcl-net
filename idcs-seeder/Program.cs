using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace EDCL.IdcsSeeder;

class Program
{
    private const string ConnectionString = "Server=localhost,1466;Database=IDCS;User Id=sa;Password=IdcsPassword123!;TrustServerCertificate=True;";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== EDCL IDCS Data Seeder ===");
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- [manifest|part|kanban|skid|out-of-order|race-condition|init]");
            return;
        }

        try
        {
            await EnsureDatabaseAndTablesAsync();

            var action = args[0].ToLower();
            switch (action)
            {
                case "init":
                    Console.WriteLine("Database and tables initialized. CDC Enabled.");
                    break;
                case "manifest":
                    await SeedManifestAsync();
                    break;
                case "part":
                    await SeedPartAsync();
                    break;
                case "kanban":
                    await SeedKanbanAsync();
                    break;
                case "skid":
                    await SeedSkidAsync();
                    break;
                case "out-of-order":
                    await SeedOutOfOrderAsync();
                    break;
                case "race-condition":
                    await SeedRaceConditionAsync();
                    break;
                default:
                    Console.WriteLine($"Unknown action: {action}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task EnsureDatabaseAndTablesAsync()
    {
        var masterConnStr = "Server=localhost,1466;Database=master;User Id=sa;Password=IdcsPassword123!;TrustServerCertificate=True;";
        using (var masterConn = new SqlConnection(masterConnStr))
        {
            await masterConn.OpenAsync();
            var dbExists = await masterConn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM sys.databases WHERE name = 'IDCS'");
            if (dbExists == 0)
            {
                await masterConn.ExecuteAsync("CREATE DATABASE IDCS");
                Console.WriteLine("Created IDCS database.");
            }
        }

        using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync();
            
            // Enable CDC on Database Level
            try
            {
                var isCdcEnabled = await conn.ExecuteScalarAsync<bool>("SELECT is_cdc_enabled FROM sys.databases WHERE name = 'IDCS'");
                if (!isCdcEnabled)
                {
                    await conn.ExecuteAsync("EXEC sys.sp_cdc_enable_db");
                    Console.WriteLine("Enabled CDC on IDCS database.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: Could not enable CDC on DB level. Ensure SQL Server Agent is running. Error: " + ex.Message);
            }
            
            // Create manifests table
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='manifests' AND xtype='U')
                CREATE TABLE manifests (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    ManifestNo NVARCHAR(50) NOT NULL,
                    SupplierCode NVARCHAR(50),
                    SupplierName NVARCHAR(100),
                    Sequence INT,
                    OrderType NVARCHAR(20),
                    PickDate DATETIME,
                    Cycle NVARCHAR(20),
                    Status NVARCHAR(50)
                )");
            await EnableCdcOnTableAsync(conn, "manifests");

            // Create manifest_parts table
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='manifest_parts' AND xtype='U')
                CREATE TABLE manifest_parts (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    ManifestId BIGINT,
                    PartNo NVARCHAR(50) NOT NULL,
                    PartName NVARCHAR(100),
                    Qty INT,
                    Uom NVARCHAR(20)
                )");
            await EnableCdcOnTableAsync(conn, "manifest_parts");

            // Create manifest_kanbans table
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='manifest_kanbans' AND xtype='U')
                CREATE TABLE manifest_kanbans (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    ManifestId BIGINT,
                    PartNo NVARCHAR(50),
                    KanbanCd NVARCHAR(50)
                )");
            await EnableCdcOnTableAsync(conn, "manifest_kanbans");

            // Create manifest_skids table
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='manifest_skids' AND xtype='U')
                CREATE TABLE manifest_skids (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    ManifestId BIGINT,
                    SkidNo NVARCHAR(50)
                )");
            await EnableCdcOnTableAsync(conn, "manifest_skids");

            // Create write-back table for EDCL Delivery Status
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='edcl_delivery_status' AND xtype='U')
                CREATE TABLE edcl_delivery_status (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    ManifestId BIGINT,
                    ManifestNo NVARCHAR(50),
                    Status NVARCHAR(50),
                    DeliveredAt DATETIME,
                    Remarks NVARCHAR(MAX)
                )");
        }
    }

    private static async Task EnableCdcOnTableAsync(SqlConnection conn, string tableName)
    {
        try
        {
            var isTableCdcEnabled = await conn.ExecuteScalarAsync<bool>(
                "SELECT is_tracked_by_cdc FROM sys.tables WHERE name = @TableName", new { TableName = tableName });
                
            if (!isTableCdcEnabled)
            {
                await conn.ExecuteAsync($@"
                    EXEC sys.sp_cdc_enable_table
                    @source_schema = N'dbo',
                    @source_name   = N'{tableName}',
                    @role_name     = NULL,
                    @supports_net_changes = 0;");
                Console.WriteLine($"Enabled CDC on table {tableName}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not enable CDC on {tableName}. Error: {ex.Message}");
        }
    }

    private static async Task SeedManifestAsync()
    {
        using var conn = new SqlConnection(ConnectionString);
        var manifestNo = $"MNF-{DateTime.Now:yyyyMMddHHmmss}";
        var pickDate = DateTime.Now;
        
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifests (ManifestNo, SupplierCode, SupplierName, Sequence, OrderType, PickDate, Cycle, Status) 
            OUTPUT INSERTED.Id
            VALUES (@ManifestNo, 'SUP-001', 'Test Supplier', 1, 'ORG', @PickDate, 'C1', 'Pending')",
            new { ManifestNo = manifestNo, PickDate = pickDate });
            
        Console.WriteLine($"Inserted Manifest: {manifestNo} with ID {id}");
    }

    private static async Task SeedPartAsync()
    {
        using var conn = new SqlConnection(ConnectionString);
        var manifestId = await conn.QueryFirstOrDefaultAsync<long?>("SELECT TOP 1 Id FROM manifests ORDER BY Id DESC");
        
        if (manifestId == null)
        {
            Console.WriteLine("No manifests found. Please seed a manifest first.");
            return;
        }

        var partNo = $"PRT-{new Random().Next(1000, 9999)}";
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_parts (ManifestId, PartNo, PartName, Qty, Uom) 
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, @PartNo, 'Test Part', 10, 'PCS')",
            new { ManifestId = manifestId, PartNo = partNo });
            
        Console.WriteLine($"Inserted Part: {partNo} with ID {id} for ManifestId {manifestId}");
    }

    private static async Task SeedKanbanAsync()
    {
        using var conn = new SqlConnection(ConnectionString);
        var manifestId = await conn.QueryFirstOrDefaultAsync<long?>("SELECT TOP 1 Id FROM manifests ORDER BY Id DESC");
        
        if (manifestId == null)
        {
            Console.WriteLine("No manifests found.");
            return;
        }

        var kanban = $"KBN-{new Random().Next(1000, 9999)}";
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_kanbans (ManifestId, PartNo, KanbanCd) 
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, 'PRT-XXXX', @KanbanCd)",
            new { ManifestId = manifestId, KanbanCd = kanban });
            
        Console.WriteLine($"Inserted Kanban: {kanban} with ID {id}");
    }

    private static async Task SeedSkidAsync()
    {
        using var conn = new SqlConnection(ConnectionString);
        var manifestId = await conn.QueryFirstOrDefaultAsync<long?>("SELECT TOP 1 Id FROM manifests ORDER BY Id DESC");
        
        if (manifestId == null)
        {
            Console.WriteLine("No manifests found.");
            return;
        }

        var skid = $"SKD-{new Random().Next(1000, 9999)}";
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_skids (ManifestId, SkidNo) 
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, @SkidNo)",
            new { ManifestId = manifestId, SkidNo = skid });
            
        Console.WriteLine($"Inserted Skid: {skid} with ID {id}");
    }

    private static async Task SeedOutOfOrderAsync()
    {
        using var conn = new SqlConnection(ConnectionString);
        
        var fakeManifestId = 999999L;
        var partNo = $"PRT-OOO";
        
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_parts (ManifestId, PartNo, PartName, Qty, Uom) 
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, @PartNo, 'Out of order part', 5, 'PCS')",
            new { ManifestId = fakeManifestId, PartNo = partNo });
            
        Console.WriteLine($"[OUT-OF-ORDER] Inserted Part: {partNo} with ID {id} for non-existent ManifestId {fakeManifestId}.");
        Console.WriteLine("Check EDCL logs for Retry and SSE Fault events.");
    }

    private static async Task SeedRaceConditionAsync()
    {
        using var conn = new SqlConnection(ConnectionString);
        
        var futureManifestId = 50000L + new Random().Next(1, 9999);
        var partNo = $"PRT-RACE";
        var manifestNo = $"MNF-RACE-{futureManifestId}";
        var pickDate = DateTime.Now;
        
        await conn.OpenAsync();
        
        var partId = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_parts (ManifestId, PartNo, PartName, Qty, Uom) 
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, @PartNo, 'Race Condition Part', 5, 'PCS')",
            new { ManifestId = futureManifestId, PartNo = partNo });
            
        Console.WriteLine($"[RACE-CONDITION] Step 1: Inserted Part {partNo} for future ManifestId {futureManifestId}.");
        Console.WriteLine($"[RACE-CONDITION] Waiting 3 seconds to let MassTransit fail and start Retry...");
        
        await Task.Delay(3000);
        
        await conn.ExecuteAsync(@"
            SET IDENTITY_INSERT manifests ON;
            
            INSERT INTO manifests (Id, ManifestNo, SupplierCode, SupplierName, Sequence, OrderType, PickDate, Cycle, Status) 
            VALUES (@Id, @ManifestNo, 'SUP-002', 'Race Supplier', 1, 'ORG', @PickDate, 'C1', 'Pending');
            
            SET IDENTITY_INSERT manifests OFF;",
            new { Id = futureManifestId, ManifestNo = manifestNo, PickDate = pickDate });
            
        Console.WriteLine($"[RACE-CONDITION] Step 2: Inserted Manifest {manifestNo} with ID {futureManifestId}.");
        Console.WriteLine($"[RACE-CONDITION] Watch EDCL logs: the next retry for Part {partNo} should now SUCCESS!");
    }
}
