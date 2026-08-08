using System;
using System.Data.SqlClient;

class Program {
    static void Main() {
        var connStr = "Server=127.0.0.1,1444;Database=edcl;User Id=sa;Password=EdclMini_123!;TrustServerCertificate=True;Encrypt=False;";
        using var conn = new SqlConnection(connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM edcl.ingestion.sync_sessions";
        var countSync = cmd.ExecuteScalar();
        cmd.CommandText = "SELECT COUNT(*) FROM edcl.job.pickup_orders";
        var countPickup = cmd.ExecuteScalar();
        Console.WriteLine($"SyncSessions: {countSync}, PickupOrders: {countPickup}");
    }
}
