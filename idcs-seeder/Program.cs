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
            Console.WriteLine("Usage: dotnet run -- [init|manifest|part|kanban|skid|bulk|reset|out-of-order|race-condition|edcl-master]");
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
                case "supplier":
                    await SeedSupplierAsync();
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
                case "bulk":
                    await SeedBulkAsync();
                    break;
                case "edcl-master":
                    await SeedEdclMasterAsync();
                    break;
                case "reset":
                    await ResetDataAsync();
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
        using var conn = new SqlConnection(ConnectionString);
        var supplierCode = $"SUP-{new Random().Next(100, 999)}";
        
        var id = await conn.QuerySingleAsync<long>(@"
            INSERT INTO suppliers (SupplierCode, SupplierName, Address)
            OUTPUT INSERTED.Id
            VALUES (@SupplierCode, 'Test Supplier ' + @SupplierCode, 'Jl. Industri No. 1, Cikarang')",
            new { SupplierCode = supplierCode });

        using var edclConn = new SqlConnection(EdclConnectionString);
        await edclConn.ExecuteAsync(@"
            IF NOT EXISTS(SELECT 1 FROM driver.suppliers WHERE SupplierCode = @SupplierCode)
            BEGIN
                INSERT INTO driver.suppliers (SupplierCode, Name, Address, Latitude, Longitude, GeofenceRadiusMeters, IsActive, created_at, created_by, is_deleted)
                VALUES (@SupplierCode, 'Test Supplier ' + @SupplierCode, 'Jl. Industri No. 1, Cikarang', -6.3, 107.1, 100, 1, GETUTCDATE(), 'System', 0)
            END", new { SupplierCode = supplierCode });

        Console.WriteLine($"Inserted Supplier: {supplierCode} with ID {id}");
    }

    // ─── Static realistic data tables (based on data-real analysis) ─────────
    private static readonly (string Code, string Name, string Plant, string Dock, string PLane)[] SupplierData = new[]
    {
        ("5147", "TG INOAC INDONESIA",          "2", "54", "RD23"),
        ("5159", "ADVICS INDONESIA",             "4", "C3", "ME53"),
        ("0003", "SUGITY CREATIVES",             "6", "53", "ME53"),
        ("5566", "DENSO MANUFACTURING INDONESIA","1", "53", "RD23"),
        ("T060", "TOYOTA BOSHOKU INDONESIA",     "1", "56", "ME56"),
        ("5626", "AISIN AW INDONESIA",           "1", "C3", "ME53"),
    };

    private static readonly string[] PartNames = new[]
    {
        "RUN  FR DOOR GLASS  RH",  "RUN  FR DOOR GLASS  LH",
        "RUN  RR DOOR GLASS  RH",  "RUN  RR DOOR GLASS  LH",
        "CYLINDER ASSY  BRAKE MASTER", "PANEL  CONSOLE RR END",
        "BOX ASSY  CONSOLE  RR",   "PANEL SUB-ASSY  CONSOLE  UPR",
        "COVER  CONSOLE BOX HOLE", "INSERT  CONSOLE BOX  RR",
    };
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedManifestAsync()
    {
        var rnd = new Random();
        using var conn = new SqlConnection(ConnectionString);
        var supplier = SupplierData[rnd.Next(SupplierData.Length)];
        // ManifestNo format: 524 + 7 random digits, mirroring '5240093742'
        var manifestNo = $"524{rnd.Next(0, 9999999):D7}";
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
                  SupplierPlant = supplier.Plant, Sequence = rnd.Next(1, 25), PickDate = pickDate });

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
            foreach (var s in SupplierData)
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
            var supplier   = SupplierData[rnd.Next(SupplierData.Length)];
            // ManifestNo: 524 + 7 digit, mirroring real data like '5240093742'
            var manifestNo = $"524{rnd.Next(0, 9999999):D7}";
            var seq        = rnd.Next(1, 25);

            var manifestId = await conn.QuerySingleAsync<long>(@"
                INSERT INTO manifests (ManifestNo, SupplierCode, SupplierName, SupplierPlant, Sequence, OrderType, PickDate, Cycle, Status)
                OUTPUT INSERTED.Id
                VALUES (@ManifestNo, @SupplierCode, @SupplierName, @SupplierPlant, @Sequence, '1', @PickDate, 'C1', 'Pending')",
                new { ManifestNo = manifestNo, SupplierCode = supplier.Code, SupplierName = supplier.Name,
                      SupplierPlant = supplier.Plant, Sequence = seq, PickDate = DateTime.Now });

            // 1 Skid per Manifest — format: SKD + 4 alphanumeric
            var skidNo = $"SKD{rnd.Next(1000, 9999)}";
            await conn.ExecuteAsync(@"
                INSERT INTO manifest_skids (ManifestId, SkidNo) VALUES (@ManifestId, @SkidNo)",
                new { ManifestId = manifestId, SkidNo = skidNo });

            // 5–10 Parts per Manifest — realistic PartNo format: 6digits + 1letter + 5digits
            int partCount = rnd.Next(5, 11);
            for (int p = 1; p <= partCount; p++)
            {
                // e.g. '681410D25000'
                var partNo   = $"{rnd.Next(100000, 999999)}{(char)('A' + rnd.Next(26))}{rnd.Next(10000, 99999)}";
                var partName = PartNames[rnd.Next(PartNames.Length)];
                // KanbanNo: 1 letter + 3 digits, e.g. 'F585'
                var kanbanNo = $"{(char)('A' + rnd.Next(26))}{rnd.Next(100, 999)}";

                await conn.ExecuteAsync(@"
                    INSERT INTO manifest_parts (ManifestId, PartNo, PartName, Qty, Uom)
                    VALUES (@ManifestId, @PartNo, @PartName, @Qty, 'PCS')",
                    new { ManifestId = manifestId, PartNo = partNo, PartName = partName, Qty = rnd.Next(1, 33) });

                // 1–3 Kanbans per Part — KanbanCd: 'K' + 5 digits, e.g. 'K00001'
                int kanbanCount = rnd.Next(1, 4);
                for (int k = 1; k <= kanbanCount; k++)
                {
                    var kanbanCd = $"K{rnd.Next(1, 99999):D5}";
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
        var partNo   = $"{rnd.Next(100000, 999999)}{(char)('A' + rnd.Next(26))}{rnd.Next(10000, 99999)}";
        var partName = PartNames[rnd.Next(PartNames.Length)];
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
        partNo ??= $"{rnd.Next(100000, 999999)}{(char)('A' + rnd.Next(26))}{rnd.Next(10000, 99999)}";

        // KanbanCd: 'K' + 5 digits, e.g. 'K00023'
        var kanbanCd = $"K{rnd.Next(1, 99999):D5}";
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
        var partNo           = $"{rnd.Next(100000, 999999)}{(char)('A' + rnd.Next(26))}{rnd.Next(10000, 99999)}";
        var manifestNo       = $"524{futureManifestId:D7}";
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

    private static async Task SeedEdclMasterAsync()
    {
        using var conn = new SqlConnection(EdclConnectionString);
        await conn.OpenAsync();

        Console.WriteLine("Seeding Transporter...");
        var transporterId = await conn.ExecuteScalarAsync<long?>(
            "SELECT Id FROM edcl.driver.transporters WHERE Name = 'Hikari Logistics'");
        if (transporterId == null)
        {
            transporterId = await conn.QuerySingleAsync<long>(@"
                INSERT INTO edcl.driver.transporters (Name, created_at, created_by, is_deleted) 
                OUTPUT INSERTED.Id 
                VALUES ('Hikari Logistics', GETUTCDATE(), 'System', 0)");
            Console.WriteLine($"Inserted Transporter ID: {transporterId}");
        }
        else Console.WriteLine($"Transporter already exists. ID: {transporterId}");

        Console.WriteLine("Seeding Truck...");
        var truckId = await conn.ExecuteScalarAsync<long?>(
            "SELECT Id FROM edcl.driver.trucks WHERE PlateNumber = 'B 9607 PXT'");
        if (truckId == null)
        {
            truckId = await conn.QuerySingleAsync<long>(@"
                INSERT INTO edcl.driver.trucks (TransporterId, PlateNumber, VehicleType, created_at, created_by, is_deleted) 
                OUTPUT INSERTED.Id 
                VALUES (@TransporterId, 'B 9607 PXT', 'Wingbox', GETUTCDATE(), 'System', 0)",
                new { TransporterId = transporterId });
            Console.WriteLine($"Inserted Truck ID: {truckId}");
        }
        else Console.WriteLine($"Truck already exists. ID: {truckId}");

        Console.WriteLine("Seeding Driver...");
        var driverId = await conn.ExecuteScalarAsync<long?>(
            "SELECT Id FROM edcl.auth.drivers WHERE Nik = '3201012345678901'");
        if (driverId == null)
        {
            // Bcrypt hash for '123456'
            var pinHash = "$2b$12$V4UgAH0Af5i1aIkofUcN9OQ/ZF4TQRmklC1TajvEV6urRg6m7KnmO";
            driverId = await conn.QuerySingleAsync<long>(@"
                INSERT INTO edcl.auth.drivers (TransporterId, Name, Nik, PhoneNumber, PinHash, IsActive, created_at, created_by, is_deleted) 
                OUTPUT INSERTED.Id 
                VALUES (@TransporterId, 'LISTIONO', '3201012345678901', '081234567890', @PinHash, 1, GETUTCDATE(), 'System', 0)",
                new { TransporterId = transporterId, PinHash = pinHash });
            Console.WriteLine($"Inserted Driver ID: {driverId}");
        }
        else Console.WriteLine($"Driver already exists. ID: {driverId}");

        Console.WriteLine("EDCL Master Data seeding completed successfully.");
    }
}
