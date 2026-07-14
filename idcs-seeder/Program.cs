using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace EDCL.IdcsSeeder;

class Program
{
    private const string ConnectionString = "Server=127.0.0.1,1466;Database=IDCS;User Id=sa;Password=IdcsPassword123!;TrustServerCertificate=True;";
    private const string EdclConnectionString = "Server=127.0.0.1,1444;Database=edcl;User Id=sa;Password=EdclMini_123!;TrustServerCertificate=True;";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== EDCL IDCS Data Seeder ===");
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dotnet run -- [init|manifest|part|kanban|skid|bulk|reset|out-of-order|race-condition|one-master|logistic-partner|reset-logistic-partner|route|reset-route|driver|reset-driver|reset-pickup|trigger-reject]");
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
                case "transaction":
                    await SeedManifestAsync();
                    await SeedSkidAsync();
                    await SeedPartAsync();
                    await SeedKanbanAsync();
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
                case "reset-pickup":
                    await ResetPickupAsync();
                    break;
                case "bulk":
                    await SeedBulkAsync();
                    break;
                case "one-master":
                    await SeedOneMasterAsync();
                    break;
                case "all":
                    await SeedLogisticPartnerAsync();
                    await SeedSupplierAsync();
                    await SeedRouteAsync();
                    await SeedDriverAsync();
                    await SeedTruckAsync();
                    break;
                case "reset-all":
                    await ResetTruckAsync();
                    await ResetDriverAsync();
                    await ResetRouteAsync();
                    await ResetSupplierAsync();
                    await ResetLogisticPartnerAsync();
                    break;
                case "supplier":
                    await SeedSupplierAsync();
                    break;
                case "reset-supplier":
                    await ResetSupplierAsync();
                    break;
                case "logistic-partner":
                    await SeedLogisticPartnerAsync();
                    break;
                case "reset-logistic-partner":
                    await ResetLogisticPartnerAsync();
                    break;
                case "route":
                    await SeedRouteAsync();
                    break;
                case "reset-route":
                    await ResetRouteAsync();
                    break;
                case "driver":
                    await SeedDriverAsync();
                    break;
                case "reset-driver":
                    await ResetDriverAsync();
                    break;
                case "truck":
                    await SeedTruckAsync();
                    break;
                case "reset-truck":
                    await ResetTruckAsync();
                    break;
                case "reset":
                    await ResetDataAsync();
                    break;
                case "trigger-reject":
                    await TriggerRejectAsync();
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
        var masterConnStr = "Server=127.0.0.1,1466;Database=master;User Id=sa;Password=IdcsPassword123!;TrustServerCertificate=True;";
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
            
            // Create suppliers table
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='suppliers' AND xtype='U')
                CREATE TABLE suppliers (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    SupplierCode NVARCHAR(50) NOT NULL,
                    SupplierName NVARCHAR(100),
                    Address NVARCHAR(500)
                )");
            await EnableCdcOnTableAsync(conn, "suppliers");

            // Create manifests table
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='manifests' AND xtype='U')
                CREATE TABLE manifests (
                    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                    ManifestNo NVARCHAR(50) NOT NULL,
                    SupplierCode NVARCHAR(50),
                    SupplierName NVARCHAR(100),
                    SupplierPlant NVARCHAR(1),
                    Sequence INT,
                    OrderType NVARCHAR(20),
                    PickDate DATETIME,
                    Cycle NVARCHAR(20),
                    Status NVARCHAR(50)
                )");
            // Add SupplierPlant column if the table already exists without it
            await conn.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('manifests') AND name = 'SupplierPlant')
                    ALTER TABLE manifests ADD SupplierPlant NVARCHAR(1) NULL");
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

    private static async Task SeedSupplierAsync()
    {
        Console.WriteLine("Seeding Suppliers into EDCL & IDCS...");
        using var connIdcs = new SqlConnection(ConnectionString);
        using var connEdcl = new SqlConnection(EdclConnectionString);
        await connIdcs.OpenAsync();
        await connEdcl.OpenAsync();

        int count = 0;
        foreach (var supplier in SupplierMasterData.Data)
        {
            // Insert into IDCS
            var idcsExists = await connIdcs.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM suppliers WHERE SupplierCode = @Code",
                new { Code = supplier.Code });
            
            if (!idcsExists)
            {
                await connIdcs.ExecuteAsync(@"
                    INSERT INTO suppliers (SupplierCode, SupplierName, Address)
                    VALUES (@Code, @Name, 'Jl. Industri No. 1, Cikarang')",
                    new { Code = supplier.Code, Name = supplier.Name });
            }

            // Insert into EDCL
            var edclExists = await connEdcl.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.driver.suppliers WHERE SupplierCode = @Code",
                new { Code = supplier.Code });

            if (!edclExists)
            {
                await connEdcl.ExecuteAsync(@"
                    INSERT INTO edcl.driver.suppliers (SupplierCode, Name, Address, Latitude, Longitude, GeofenceRadiusMeters, IsActive, created_at, created_by, is_deleted)
                    VALUES (@Code, @Name, 'Jl. Industri No. 1, Cikarang', -6.3, 107.1, 100, 1, GETUTCDATE(), 'System', 0)",
                    new { Code = supplier.Code, Name = supplier.Name });
            }
            count++;
        }

        Console.WriteLine($"✅ Seeded {count} Suppliers.");
    }

    private static async Task ResetSupplierAsync()
    {
        Console.WriteLine("⚠️  Starting reset for Suppliers in EDCL & IDCS...");
        using var connIdcs = new SqlConnection(ConnectionString);
        using var connEdcl = new SqlConnection(EdclConnectionString);
        await connIdcs.OpenAsync();
        await connEdcl.OpenAsync();

        try
        {
            await connIdcs.ExecuteAsync("DELETE FROM suppliers");
            await connIdcs.ExecuteAsync("DBCC CHECKIDENT ('suppliers', RESEED, 0)");
            
            await connEdcl.ExecuteAsync("DELETE FROM edcl.driver.suppliers");
            await connEdcl.ExecuteAsync("DBCC CHECKIDENT ('edcl.driver.suppliers', RESEED, 0)");
            
            Console.WriteLine("✅ Reset complete. Supplier data cleared.");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"❌ Reset failed. Error: {ex.Message}");
        }
    }


    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedManifestAsync()
    {
        var rnd = new Random();
        using var conn = new SqlConnection(ConnectionString);
        var mData = ManifestData.Data[rnd.Next(ManifestData.Data.Count)];
        var supplier = (Code: mData.SupplierCode, Name: mData.SupplierName, Plant: mData.SupplierPlant);
        var manifestNo = mData.ManifestNo;
        var seq = mData.Sequence;
        var pickDate   = DateTime.Now;

        await conn.ExecuteAsync(@"
            IF NOT EXISTS(SELECT 1 FROM suppliers WHERE SupplierCode = @Code)
                INSERT INTO suppliers (SupplierCode, SupplierName, Address)
                VALUES (@Code, @Name, 'Kawasan Industri Karawang')",
            new { Code = supplier.Code, Name = supplier.Name });

        using var edclConn = new SqlConnection(EdclConnectionString);
        await edclConn.ExecuteAsync(@"
            IF NOT EXISTS(SELECT 1 FROM driver.suppliers WHERE SupplierCode = @Code)
                INSERT INTO driver.suppliers (SupplierCode, Name, Address, Latitude, Longitude, GeofenceRadiusMeters, IsActive, created_at, created_by, is_deleted)
                VALUES (@Code, @Name, 'Kawasan Industri Karawang', -6.3, 107.1, 100, 1, GETUTCDATE(), 'System', 0)",
            new { Code = supplier.Code, Name = supplier.Name });

        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifests (ManifestNo, SupplierCode, SupplierName, SupplierPlant, Sequence, OrderType, PickDate, Cycle, Status)
            OUTPUT INSERTED.Id
            VALUES (@ManifestNo, @SupplierCode, @SupplierName, @SupplierPlant, @Sequence, '1', @PickDate, 'C1', 'Pending')",
            new { ManifestNo = manifestNo, SupplierCode = supplier.Code, SupplierName = supplier.Name,
                  SupplierPlant = supplier.Plant, Sequence = seq, PickDate = pickDate });

        Console.WriteLine($"Inserted Manifest: {manifestNo} (Supplier: {supplier.Code}/{supplier.Name}, Plant: {supplier.Plant}) with ID {id}");
    }

    private static async Task SeedBulkAsync()
    {
        var rnd = new Random();
        Console.WriteLine("Starting bulk seed of 200 manifests with realistic data...");

        // Seed all unique suppliers to IDCS + EDCL first
        using (var conn = new SqlConnection(ConnectionString))
        using (var edclConn = new SqlConnection(EdclConnectionString))
        {
            foreach (var s in SupplierMasterData.Data)
            {
                await conn.ExecuteAsync(@"
                    IF NOT EXISTS(SELECT 1 FROM suppliers WHERE SupplierCode = @Code)
                        INSERT INTO suppliers (SupplierCode, SupplierName, Address)
                        VALUES (@Code, @Name, 'Kawasan Industri Karawang')",
                    new { Code = s.Code, Name = s.Name });

                await edclConn.ExecuteAsync(@"
                    IF NOT EXISTS(SELECT 1 FROM driver.suppliers WHERE SupplierCode = @Code)
                        INSERT INTO driver.suppliers (SupplierCode, Name, Address, Latitude, Longitude, GeofenceRadiusMeters, IsActive, created_at, created_by, is_deleted)
                        VALUES (@Code, @Name, 'Kawasan Industri Karawang', -6.3, 107.1, 100, 1, GETUTCDATE(), 'System', 0)",
                    new { Code = s.Code, Name = s.Name });
            }
        }

        for (int m = 1; m <= 200; m++)
        {
            using var conn = new SqlConnection(ConnectionString);
            var mData = ManifestData.Data[m - 1];
            var supplier = (Code: mData.SupplierCode, Name: mData.SupplierName, Plant: mData.SupplierPlant);
            var manifestNo = mData.ManifestNo;
            var seq = mData.Sequence;

            var manifestId = await conn.QuerySingleAsync<long>(@"
                INSERT INTO manifests (ManifestNo, SupplierCode, SupplierName, SupplierPlant, Sequence, OrderType, PickDate, Cycle, Status)
                OUTPUT INSERTED.Id
                VALUES (@ManifestNo, @SupplierCode, @SupplierName, @SupplierPlant, @Sequence, '1', @PickDate, 'C1', 'Pending')",
                new { ManifestNo = manifestNo, SupplierCode = supplier.Code, SupplierName = supplier.Name,
                      SupplierPlant = supplier.Plant, Sequence = seq, PickDate = DateTime.Now });

            // 1 Skid per Manifest — format: SKD + 4 alphanumeric
            var skidNo = SkidData.Data[rnd.Next(SkidData.Data.Count)];
            await conn.ExecuteAsync(@"
                INSERT INTO manifest_skids (ManifestId, SkidNo) VALUES (@ManifestId, @SkidNo)",
                new { ManifestId = manifestId, SkidNo = skidNo });

            // 5–10 Parts per Manifest — realistic PartNo format: 6digits + 1letter + 5digits
            int partCount = rnd.Next(5, 11);
            for (int p = 1; p <= partCount; p++)
            {
                // e.g. '681410D25000'
                var pData = PartData.Data[rnd.Next(PartData.Data.Count)];
        var partNo = pData.PartNo;
        var partName = pData.PartName;
                
                await conn.ExecuteAsync(@"
                    INSERT INTO manifest_parts (ManifestId, PartNo, PartName, Qty, Uom)
                    VALUES (@ManifestId, @PartNo, @PartName, @Qty, 'PCS')",
                    new { ManifestId = manifestId, PartNo = partNo, PartName = partName, Qty = rnd.Next(1, 33) });

                // 1–3 Kanbans per Part — KanbanCd: 'K' + 5 digits, e.g. 'K00001'
                int kanbanCount = rnd.Next(1, 4);
                for (int k = 1; k <= kanbanCount; k++)
                {
                    var kanbanCd = KanbanData.Data[rnd.Next(KanbanData.Data.Count)];
                    await conn.ExecuteAsync(@"
                        INSERT INTO manifest_kanbans (ManifestId, PartNo, KanbanCd)
                        VALUES (@ManifestId, @PartNo, @KanbanCd)",
                        new { ManifestId = manifestId, PartNo = partNo, KanbanCd = kanbanCd });
                }
            }

            if (m % 20 == 0)
                Console.WriteLine($"Progress: {m}/200 manifests seeded.");
        }
        Console.WriteLine("Bulk seed completed successfully!");
    }


    private static async Task SeedPartAsync()
    {
        var rnd = new Random();
        using var conn = new SqlConnection(ConnectionString);
        var manifestId = await conn.QueryFirstOrDefaultAsync<long?>("SELECT TOP 1 Id FROM manifests ORDER BY Id DESC");

        if (manifestId == null)
        {
            Console.WriteLine("No manifests found. Please seed a manifest first.");
            return;
        }

        // Realistic PartNo: 6digits + 1letter + 5digits, e.g. '681410D25000'
        var pData = PartData.Data[rnd.Next(PartData.Data.Count)];
        var partNo = pData.PartNo;
        var partName = pData.PartName;
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_parts (ManifestId, PartNo, PartName, Qty, Uom)
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, @PartNo, @PartName, @Qty, 'PCS')",
            new { ManifestId = manifestId, PartNo = partNo, PartName = partName, Qty = rnd.Next(1, 33) });

        Console.WriteLine($"Inserted Part: {partNo} ({partName}) with ID {id} for ManifestId {manifestId}");
    }

    private static async Task SeedKanbanAsync()
    {
        var rnd = new Random();
        using var conn = new SqlConnection(ConnectionString);
        var manifestId = await conn.QueryFirstOrDefaultAsync<long?>("SELECT TOP 1 Id FROM manifests ORDER BY Id DESC");

        if (manifestId == null)
        {
            Console.WriteLine("No manifests found.");
            return;
        }

        // Re-use the latest part from this manifest, or create a new one
        var partNo = await conn.QueryFirstOrDefaultAsync<string?>(
            "SELECT TOP 1 PartNo FROM manifest_parts WHERE ManifestId = @ManifestId ORDER BY Id DESC",
            new { ManifestId = manifestId });
        partNo ??= PartData.Data[rnd.Next(PartData.Data.Count)].PartNo;

        // KanbanCd: 'K' + 5 digits, e.g. 'K00023'
        var kanbanCd = KanbanData.Data[rnd.Next(KanbanData.Data.Count)];
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_kanbans (ManifestId, PartNo, KanbanCd)
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, @PartNo, @KanbanCd)",
            new { ManifestId = manifestId, PartNo = partNo, KanbanCd = kanbanCd });

        Console.WriteLine($"Inserted Kanban: {kanbanCd} (Part: {partNo}) with ID {id}");
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
        var rnd = new Random();
        using var conn = new SqlConnection(ConnectionString);

        var futureManifestId = 50000L + rnd.Next(1, 9999);
        var partNo = PartData.Data[rnd.Next(PartData.Data.Count)].PartNo;
        var manifestNo = ManifestData.Data[rnd.Next(ManifestData.Data.Count)].ManifestNo;
        var pickDate         = DateTime.Now;

        await conn.OpenAsync();

        var partId = await conn.QuerySingleAsync<long>(@"
            INSERT INTO manifest_parts (ManifestId, PartNo, PartName, Qty, Uom)
            OUTPUT INSERTED.Id
            VALUES (@ManifestId, @PartNo, 'Race Condition Part', 5, 'PCS')",
            new { ManifestId = futureManifestId, PartNo = partNo });

        Console.WriteLine($"[RACE-CONDITION] Step 1: Inserted Part {partNo} for future ManifestId {futureManifestId}.");
        Console.WriteLine($"[RACE-CONDITION] Waiting 3 seconds to let Worker fail and start Retry...");

        await Task.Delay(3000);

        await conn.ExecuteAsync(@"
            SET IDENTITY_INSERT manifests ON;

            INSERT INTO manifests (Id, ManifestNo, SupplierCode, SupplierName, SupplierPlant, Sequence, OrderType, PickDate, Cycle, Status)
            VALUES (@Id, @ManifestNo, '5566', 'DENSO MANUFACTURING INDONESIA', '1', 1, '1', @PickDate, 'C1', 'Pending');

            SET IDENTITY_INSERT manifests OFF;",
            new { Id = futureManifestId, ManifestNo = manifestNo, PickDate = pickDate });

        Console.WriteLine($"[RACE-CONDITION] Step 2: Inserted Manifest {manifestNo} with ID {futureManifestId}.");
        Console.WriteLine($"[RACE-CONDITION] Watch EDCL logs: the next retry for Part {partNo} should now SUCCESS!");
    }

    private static async Task ResetDataAsync()
    {
        Console.WriteLine("⚠️  Starting data reset for IDCS & EDCL...");

        // ── IDCS ────────────────────────────────────────────────────────────────
        Console.WriteLine("[IDCS] Deleting manifest_kanbans, manifest_parts, manifest_skids, manifests...");
        using (var idcsConn = new SqlConnection(ConnectionString))
        {
            // Delete in FK-safe order (children first)
            await idcsConn.ExecuteAsync("DELETE FROM manifest_kanbans");
            await idcsConn.ExecuteAsync("DELETE FROM manifest_parts");
            await idcsConn.ExecuteAsync("DELETE FROM manifest_skids");
            await idcsConn.ExecuteAsync("DELETE FROM manifests");
            // Reset identity seeds so IDs restart from 1
            await idcsConn.ExecuteAsync("DBCC CHECKIDENT ('manifest_kanbans', RESEED, 0)");
            await idcsConn.ExecuteAsync("DBCC CHECKIDENT ('manifest_parts',    RESEED, 0)");
            await idcsConn.ExecuteAsync("DBCC CHECKIDENT ('manifest_skids',    RESEED, 0)");
            await idcsConn.ExecuteAsync("DBCC CHECKIDENT ('manifests',          RESEED, 0)");
        }
        Console.WriteLine("[IDCS] Done.");

        // ── EDCL ingestion ──────────────────────────────────────────────────────
        Console.WriteLine("[EDCL] Deleting ingestion.ManifestKanbans, ManifestParts, ManifestSkids, Manifests...");
        using (var edclConn = new SqlConnection(EdclConnectionString))
        {
            // Delete in FK-safe order (children first)
            await edclConn.ExecuteAsync("DELETE FROM edcl.ingestion.manifest_kanbans");
            await edclConn.ExecuteAsync("DELETE FROM edcl.ingestion.manifest_parts");
            await edclConn.ExecuteAsync("DELETE FROM edcl.ingestion.manifest_skids");
            await edclConn.ExecuteAsync("DELETE FROM edcl.ingestion.manifests");
            // Reset identity seeds
            await edclConn.ExecuteAsync("DBCC CHECKIDENT ('edcl.ingestion.manifest_kanbans', RESEED, 0)");
            await edclConn.ExecuteAsync("DBCC CHECKIDENT ('edcl.ingestion.manifest_parts',    RESEED, 0)");
            await edclConn.ExecuteAsync("DBCC CHECKIDENT ('edcl.ingestion.manifest_skids',    RESEED, 0)");
            await edclConn.ExecuteAsync("DBCC CHECKIDENT ('edcl.ingestion.manifests',          RESEED, 0)");
        }
        Console.WriteLine("[EDCL] Done.");

        Console.WriteLine("✅ Reset complete. Both IDCS & EDCL manifest data cleared.");
    }

    private static async Task ResetPickupAsync()
    {
        Console.WriteLine("⚠️  Starting reset for Pickup Orders and Manifest Status...");
        using var connEdcl = new SqlConnection(EdclConnectionString);
        using var connIdcs = new SqlConnection(ConnectionString);
        await connEdcl.OpenAsync();
        await connIdcs.OpenAsync();

        // 1. Delete all pickup order transaction tables in EDCL
        try
        {
            await connEdcl.ExecuteAsync("DELETE FROM ingestion.manifest_problems");
            await connEdcl.ExecuteAsync("DBCC CHECKIDENT ('ingestion.manifest_problems', RESEED, 0)");

            await connEdcl.ExecuteAsync("DELETE FROM job.pickup_order_kanbans");
            await connEdcl.ExecuteAsync("DBCC CHECKIDENT ('job.pickup_order_kanbans', RESEED, 0)");
            
            await connEdcl.ExecuteAsync("DELETE FROM job.pickup_order_manifests");
            await connEdcl.ExecuteAsync("DBCC CHECKIDENT ('job.pickup_order_manifests', RESEED, 0)");
            
            await connEdcl.ExecuteAsync("DELETE FROM job.pickup_order_details");
            await connEdcl.ExecuteAsync("DBCC CHECKIDENT ('job.pickup_order_details', RESEED, 0)");
            
            var rows = await connEdcl.ExecuteAsync("DELETE FROM job.pickup_orders");
            await connEdcl.ExecuteAsync("DBCC CHECKIDENT ('job.pickup_orders', RESEED, 0)");
            Console.WriteLine($"✅ Cleared {rows} Pickup Orders, their children, and Manifest Problems in EDCL.");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"❌ Failed to delete Pickup Orders in EDCL: {ex.Message}");
        }

        // 2. Reset Manifests assignment in EDCL Ingestion schema
        try
        {
            var rowsEdcl = await connEdcl.ExecuteAsync("UPDATE ingestion.manifests SET IsAssignedToRoute = 0");
            Console.WriteLine($"✅ Reset {rowsEdcl} Manifests to unassigned in EDCL (ingestion.manifests).");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"❌ Failed to reset Manifests: {ex.Message}");
        }

        Console.WriteLine("[Pickup Reset] Done.");
    }

    private static async Task SeedOneMasterAsync()
    {
        using var connIdcs = new SqlConnection(ConnectionString);
        using var connEdcl = new SqlConnection(EdclConnectionString);
        await connIdcs.OpenAsync();
        await connEdcl.OpenAsync();

        Console.WriteLine("Seeding Logistic Partner...");
        var lpId = await connEdcl.ExecuteScalarAsync<long?>(
            "SELECT Id FROM edcl.driver.logistic_partners WHERE Code = 'HKR'");
        if (lpId == null)
        {
            lpId = await connEdcl.QuerySingleAsync<long>(@"
                INSERT INTO edcl.driver.logistic_partners (Code, Name, created_at, created_by, is_deleted) 
                OUTPUT INSERTED.Id 
                VALUES ('HKR', 'Hikari Logistics', GETUTCDATE(), 'System', 0)");
            Console.WriteLine($"Inserted Logistic Partner ID: {lpId}");
        }

        Console.WriteLine("Seeding Supplier...");
        var supplierEdclExists = await connEdcl.ExecuteScalarAsync<bool>(
            "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.driver.suppliers WHERE SupplierCode = '5566'");
        if (!supplierEdclExists) {
            await connEdcl.ExecuteAsync(@"
                INSERT INTO edcl.driver.suppliers (SupplierCode, Name, Address, Latitude, Longitude, GeofenceRadiusMeters, IsActive, created_at, created_by, is_deleted)
                VALUES ('5566', 'DENSO MANUFACTURING INDONESIA', 'Cikarang', -6.3, 107.1, 100, 1, GETUTCDATE(), 'System', 0)");
        }
        var supplierIdcsExists = await connIdcs.ExecuteScalarAsync<bool>(
            "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM suppliers WHERE SupplierCode = '5566'");
        if (!supplierIdcsExists) {
            await connIdcs.ExecuteAsync(@"
                INSERT INTO suppliers (SupplierCode, SupplierName, Address)
                VALUES ('5566', 'DENSO MANUFACTURING INDONESIA', 'Cikarang')");
        }

        Console.WriteLine("Seeding Route...");
        var routeExists = await connEdcl.ExecuteScalarAsync<bool>(
            "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.driver.routes WHERE RouteCode = 'R01' AND CycleCode = 'C1'");
        if (!routeExists) {
            await connEdcl.ExecuteAsync(@"
                INSERT INTO edcl.driver.routes (RouteCode, CycleCode, created_at, created_by, is_deleted)
                VALUES ('R01', 'C1', GETUTCDATE(), 'System', 0)");
        }

        Console.WriteLine("Seeding Truck...");
        var truckId = await connEdcl.ExecuteScalarAsync<long?>(
            "SELECT Id FROM edcl.driver.trucks WHERE PlateNumber = 'B 9607 PXT'");
        if (truckId == null)
        {
            truckId = await connEdcl.QuerySingleAsync<long>(@"
                INSERT INTO edcl.driver.trucks (LogisticPartnerId, PlateNumber, VehicleType, IsActive, created_at, created_by, is_deleted) 
                OUTPUT INSERTED.Id 
                VALUES (@LpId, 'B 9607 PXT', 'Wingbox', 1, GETUTCDATE(), 'System', 0)",
                new { LpId = lpId });
            Console.WriteLine($"Inserted Truck ID: {truckId}");
        }

        Console.WriteLine("Seeding Driver...");
        var driverId = await connEdcl.ExecuteScalarAsync<long?>(
            "SELECT Id FROM edcl.auth.drivers WHERE Nik = '3201012345678901'");
        if (driverId == null)
        {
            var pinHash = "$2b$12$V4UgAH0Af5i1aIkofUcN9OQ/ZF4TQRmklC1TajvEV6urRg6m7KnmO";
            driverId = await connEdcl.QuerySingleAsync<long>(@"
                INSERT INTO edcl.auth.drivers (LogisticPartnerId, Name, Nik, PhoneNumber, PinHash, must_change_pin, IsActive, created_at, created_by, is_deleted) 
                OUTPUT INSERTED.Id 
                VALUES (@LpId, 'LISTIONO', '3201012345678901', '081234567890', @PinHash, 1, 1, GETUTCDATE(), 'System', 0)",
                new { LpId = lpId, PinHash = pinHash });
            Console.WriteLine($"Inserted Driver ID: {driverId}");
        }

        Console.WriteLine("Seeding Assignment...");
        var assignmentExists = await connEdcl.ExecuteScalarAsync<bool>(
            "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.driver.truck_driver_assignments WHERE TruckId = @TruckId AND DriverId = @DriverId",
            new { TruckId = truckId, DriverId = driverId });
        if (!assignmentExists) {
            await connEdcl.ExecuteAsync(@"
                INSERT INTO edcl.driver.truck_driver_assignments (TruckId, DriverId, IsActive, AssignedAt, created_at, created_by, is_deleted)
                VALUES (@TruckId, @DriverId, 1, GETUTCDATE(), GETUTCDATE(), 'System', 0)",
                new { TruckId = truckId, DriverId = driverId });
            Console.WriteLine($"Inserted Assignment for Truck: {truckId} & Driver: {driverId}");
        }

        Console.WriteLine("EDCL Mini Master Data seeding completed successfully.");
    }
    private static async Task SeedLogisticPartnerAsync()
    {
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        Console.WriteLine("Seeding Logistic Partners...");
        int count = 0;

        foreach (var lp in LogisticPartnerData.Data)
        {
            var exists = await conn.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.driver.logistic_partners WHERE Code = @Code",
                new { Code = lp.Code });

            if (!exists)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO edcl.driver.logistic_partners (Code, Name, created_at, created_by, is_deleted)
                    VALUES (@Code, @Name, GETUTCDATE(), 'System', 0)",
                    new { Code = lp.Code, Name = lp.Name });
                count++;
            }
        }

        Console.WriteLine($"Logistic Partner seeding completed successfully. {count} new records inserted.");
    }

    private static async Task ResetLogisticPartnerAsync()
    {
        Console.WriteLine("⚠️  Starting reset for Logistic Partners in EDCL...");
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        try
        {
            var rows = await conn.ExecuteAsync("DELETE FROM edcl.driver.logistic_partners");
            await conn.ExecuteAsync("DBCC CHECKIDENT ('edcl.driver.logistic_partners', RESEED, 0)");
            Console.WriteLine($"✅ Reset complete. {rows} Logistic Partners deleted.");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"❌ Reset failed. Could not delete Logistic Partners. (Maybe they are referenced by Trucks/Drivers?) Error: {ex.Message}");
        }
    }

    private static async Task SeedRouteAsync()
    {
        Console.WriteLine("Seeding Routes into EDCL...");
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        int count = 0;
        foreach (var (routeCode, cycleCode) in RouteData.Data)
        {
            var exists = await conn.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.driver.routes WHERE RouteCode = @RouteCode AND CycleCode = @CycleCode",
                new { RouteCode = routeCode, CycleCode = cycleCode });

            if (exists) continue;

            await conn.ExecuteAsync(@"
                INSERT INTO edcl.driver.routes (RouteCode, CycleCode, created_at, created_by, is_deleted)
                VALUES (@RouteCode, @CycleCode, GETUTCDATE(), 'System', 0)",
                new { RouteCode = routeCode, CycleCode = cycleCode });

            count++;
        }
        Console.WriteLine($"✅ Seeded {count} new Routes to EDCL.");
    }

    private static async Task ResetRouteAsync()
    {
        Console.WriteLine("⚠️  Starting reset for Routes in EDCL...");
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        try
        {
            await conn.ExecuteAsync("DELETE FROM edcl.driver.routes");
            await conn.ExecuteAsync("DBCC CHECKIDENT ('edcl.driver.routes', RESEED, 0)");
            Console.WriteLine("✅ Reset complete. Route data cleared.");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"❌ Reset failed. Error: {ex.Message}");
        }
    }

    private static async Task SeedDriverAsync()
    {
        Console.WriteLine("Seeding Drivers into EDCL...");
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        var existingPhones = new HashSet<string>(
            await conn.QueryAsync<string>("SELECT PhoneNumber FROM edcl.auth.drivers WHERE IsActive = 1")
        );

        int count = 0;
        foreach (var driver in DriverData.Data)
        {
            if (existingPhones.Contains(driver.Phone))
                continue;

            var exists = await conn.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.auth.drivers WHERE Nik = @Nik",
                new { Nik = driver.Nik });

            if (exists) continue;

            // Lookup LogisticPartner
            var lpId = await conn.ExecuteScalarAsync<long?>(
                "SELECT Id FROM edcl.driver.logistic_partners WHERE Code = @Code",
                new { Code = driver.LogisticPartnerCode });

            await conn.ExecuteAsync(@"
                INSERT INTO edcl.auth.drivers (Name, Nik, PhoneNumber, PinHash, must_change_pin, LogisticPartnerId, IsActive, created_at, created_by, is_deleted)
                VALUES (@Name, @Nik, @Phone, 'MOCKED_HASH', 1, @LpId, 1, GETUTCDATE(), 'System', 0)",
                new { Name = driver.Name, Nik = driver.Nik, Phone = driver.Phone, LpId = lpId });
            
            existingPhones.Add(driver.Phone);
            count++;
        }
        Console.WriteLine($"✅ Seeded {count} new Drivers to EDCL.");
    }

    private static async Task ResetDriverAsync()
    {
        Console.WriteLine("⚠️  Starting reset for Drivers in EDCL...");
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        try
        {
            await conn.ExecuteAsync("DELETE FROM edcl.auth.drivers");
            await conn.ExecuteAsync("DBCC CHECKIDENT ('edcl.auth.drivers', RESEED, 0)");
            Console.WriteLine("✅ Reset complete. Driver data cleared.");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"❌ Reset failed. Error: {ex.Message}");
        }
    }

    private static async Task SeedTruckAsync()
    {
        Console.WriteLine("Seeding Trucks & Assignments into EDCL...");
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        int truckCount = 0;
        int assignCount = 0;
        
        var existingTrucks = new HashSet<string>(
            await conn.QueryAsync<string>("SELECT PlateNumber FROM edcl.driver.trucks WHERE IsActive = 1")
        );

        foreach (var truck in TruckData.Data)
        {
            var driverInfo = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT Id as DriverId, LogisticPartnerId FROM edcl.auth.drivers WHERE Nik = @Nik AND IsActive = 1",
                new { Nik = truck.Nik });
            
            if (driverInfo == null) continue;

            long truckId;
            if (existingTrucks.Contains(truck.PlateNumber))
            {
                var existingId = await conn.ExecuteScalarAsync<long?>(
                    "SELECT Id FROM edcl.driver.trucks WHERE PlateNumber = @PlateNumber",
                    new { PlateNumber = truck.PlateNumber });
                if (existingId == null) continue;
                truckId = existingId.Value;
            }
            else
            {
                truckId = await conn.ExecuteScalarAsync<long>(@"
                    INSERT INTO edcl.driver.trucks (PlateNumber, LogisticPartnerId, IsActive, created_at, created_by, is_deleted)
                    OUTPUT INSERTED.Id
                    VALUES (@PlateNumber, @LpId, 1, GETUTCDATE(), 'System', 0)",
                    new { PlateNumber = truck.PlateNumber, LpId = driverInfo.LogisticPartnerId });
                
                existingTrucks.Add(truck.PlateNumber);
                truckCount++;
            }

            var assignmentExists = await conn.ExecuteScalarAsync<bool>(
                "SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) FROM edcl.driver.truck_driver_assignments WHERE TruckId = @TruckId AND DriverId = @DriverId",
                new { TruckId = truckId, DriverId = driverInfo.DriverId });

            if (!assignmentExists)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO edcl.driver.truck_driver_assignments (TruckId, DriverId, IsActive, AssignedAt, created_at, created_by, is_deleted)
                    VALUES (@TruckId, @DriverId, 1, GETUTCDATE(), GETUTCDATE(), 'System', 0)",
                    new { TruckId = truckId, DriverId = driverInfo.DriverId });
                assignCount++;
            }
        }
        Console.WriteLine($"✅ Seeded {truckCount} new Trucks and {assignCount} Assignments to EDCL.");
    }

    private static async Task ResetTruckAsync()
    {
        Console.WriteLine("⚠️  Starting reset for Trucks & Assignments in EDCL...");
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        try
        {
            await conn.ExecuteAsync("DELETE FROM edcl.driver.truck_driver_assignments");
            await conn.ExecuteAsync("DBCC CHECKIDENT ('edcl.driver.truck_driver_assignments', RESEED, 0)");
            await conn.ExecuteAsync("DELETE FROM edcl.driver.trucks");
            await conn.ExecuteAsync("DBCC CHECKIDENT ('edcl.driver.trucks', RESEED, 0)");
            Console.WriteLine("✅ Reset complete. Truck data cleared.");
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"❌ Reset failed. Error: {ex.Message}");
        }
    }

    private static async Task TriggerRejectAsync()
    {
        Console.WriteLine("⚠️  Seeding mock InTransit (ON_PROGRESS) and Delivered (COMPLETED) transactions in EDCL...");

        var manifestProg = "TRJ-PROG-" + new Random().Next(1000, 9999);
        var manifestComp = "TRJ-COMP-" + new Random().Next(1000, 9999);
        var manifestDel = "TRJ-DEL-" + new Random().Next(1000, 9999);
        var manifestDelC = "TRJ-DELC-" + new Random().Next(1000, 9999);

        using var edclConn = new SqlConnection(EdclConnectionString);
        await edclConn.OpenAsync();

        // 1. Seed EDCL Job schema directly
        var sqlSeedEdcl = @"
            DECLARE @PoProgId BIGINT, @PoCompId BIGINT, @PoDelId BIGINT, @PoDelCId BIGINT;
            DECLARE @DetProgId BIGINT, @DetCompId BIGINT, @DetDelId BIGINT, @DetDelCId BIGINT;

            -- Create ON_PROGRESS order for UPDATE test
            INSERT INTO edcl.job.pickup_orders (delivery_no, pickup_date, route_code, cycle_code, estimated_departure_time, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES ('PO-' + @ManifestProg, GETUTCDATE(), 'R-TEST', 'C1', '08:00:00', 'ON_PROGRESS', GETUTCDATE(), 'Seeder', 0);
            SET @PoProgId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_details (PickupOrderId, SupplierId, Sequence, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@PoProgId, 1, 1, 'PENDING', GETUTCDATE(), 'Seeder', 0);
            SET @DetProgId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_manifests (PickupOrderDetailId, ManifestNo, TotalKanban, TotalSkid, ScannedKanban, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@DetProgId, @ManifestProg, 1, 1, 0, 'PENDING', GETUTCDATE(), 'Seeder', 0);

            -- Create COMPLETED order for UPDATE test
            INSERT INTO edcl.job.pickup_orders (delivery_no, pickup_date, route_code, cycle_code, estimated_departure_time, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES ('PO-' + @ManifestComp, GETUTCDATE(), 'R-TEST', 'C1', '08:00:00', 'COMPLETED', GETUTCDATE(), 'Seeder', 0);
            SET @PoCompId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_details (PickupOrderId, SupplierId, Sequence, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@PoCompId, 1, 1, 'PICKED_UP', GETUTCDATE(), 'Seeder', 0);
            SET @DetCompId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_manifests (PickupOrderDetailId, ManifestNo, TotalKanban, TotalSkid, ScannedKanban, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@DetCompId, @ManifestComp, 1, 1, 0, 'VERIFIED', GETUTCDATE(), 'Seeder', 0);

            -- Create ON_PROGRESS order for DELETE test
            INSERT INTO edcl.job.pickup_orders (delivery_no, pickup_date, route_code, cycle_code, estimated_departure_time, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES ('PO-' + @ManifestDel, GETUTCDATE(), 'R-TEST', 'C1', '08:00:00', 'ON_PROGRESS', GETUTCDATE(), 'Seeder', 0);
            SET @PoDelId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_details (PickupOrderId, SupplierId, Sequence, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@PoDelId, 1, 1, 'PENDING', GETUTCDATE(), 'Seeder', 0);
            SET @DetDelId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_manifests (PickupOrderDetailId, ManifestNo, TotalKanban, TotalSkid, ScannedKanban, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@DetDelId, @ManifestDel, 1, 1, 0, 'PENDING', GETUTCDATE(), 'Seeder', 0);

            -- Create COMPLETED order for DELETE test
            INSERT INTO edcl.job.pickup_orders (delivery_no, pickup_date, route_code, cycle_code, estimated_departure_time, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES ('PO-' + @ManifestDelC, GETUTCDATE(), 'R-TEST', 'C1', '08:00:00', 'COMPLETED', GETUTCDATE(), 'Seeder', 0);
            SET @PoDelCId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_details (PickupOrderId, SupplierId, Sequence, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@PoDelCId, 1, 1, 'PICKED_UP', GETUTCDATE(), 'Seeder', 0);
            SET @DetDelCId = SCOPE_IDENTITY();

            INSERT INTO edcl.job.pickup_order_manifests (PickupOrderDetailId, ManifestNo, TotalKanban, TotalSkid, ScannedKanban, Status, CreatedAt, CreatedBy, IsDeleted)
            VALUES (@DetDelCId, @ManifestDelC, 1, 1, 0, 'VERIFIED', GETUTCDATE(), 'Seeder', 0);
        ";

        await edclConn.ExecuteAsync(sqlSeedEdcl, new { ManifestProg = manifestProg, ManifestComp = manifestComp, ManifestDel = manifestDel, ManifestDelC = manifestDelC });
        Console.WriteLine($"✅ Seeded EDCL jobs for {manifestProg}, {manifestComp}, {manifestDel}, and {manifestDelC}.");

        // 2. Seed into IDCS to trigger CDC Insert, then wait, then trigger CDC Update/Delete
        Console.WriteLine($"⚠️  Inserting these manifests into IDCS database to trigger CDC...");
        using var idcsConn = new SqlConnection(ConnectionString);
        await idcsConn.OpenAsync();
        
        var sqlInsertIdcs = @"
            INSERT INTO manifests (ManifestNo, SupplierCode, SupplierName, SupplierPlant, Sequence, OrderType, PickDate, Cycle, Status)
            VALUES (@ManifestNo, 'SUP-01', 'Test Supplier', '1', 1, '1', GETDATE(), 'C1', 'Pending')";

        await idcsConn.ExecuteAsync(sqlInsertIdcs, new { ManifestNo = manifestProg });
        await idcsConn.ExecuteAsync(sqlInsertIdcs, new { ManifestNo = manifestComp });
        await idcsConn.ExecuteAsync(sqlInsertIdcs, new { ManifestNo = manifestDel });
        await idcsConn.ExecuteAsync(sqlInsertIdcs, new { ManifestNo = manifestDelC });

        Console.WriteLine("✅ IDCS record inserted. Waiting 3 seconds for CDC Debezium to catch up...");
        await Task.Delay(3000);

        Console.WriteLine("⚠️  Now UPDATING them in IDCS to trigger the real rejection...");
        var sqlUpdate = @"
            UPDATE manifests 
            SET Status = 'Modified By Seeder', 
                SupplierName = 'Triggered Reject ' + CAST(NEWID() AS NVARCHAR(36))
            WHERE ManifestNo IN (@ManifestProg, @ManifestComp)";

        await idcsConn.ExecuteAsync(sqlUpdate, new { ManifestProg = manifestProg, ManifestComp = manifestComp });
        
        Console.WriteLine("⚠️  Now DELETING the last ones in IDCS to trigger a delete rejection...");
        var sqlDelete = "DELETE FROM manifests WHERE ManifestNo IN (@ManifestDel, @ManifestDelC)";
        await idcsConn.ExecuteAsync(sqlDelete, new { ManifestDel = manifestDel, ManifestDelC = manifestDelC });

        Console.WriteLine($"\n✅ Trigger finished! Check EDCL Ingestion Worker logs. It should insert them into ingestion.manifest_problems with explicit reasons.");
    }
}
