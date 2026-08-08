using System;
using System.Data.SqlClient;
using Dapper;

var connStr = "Server=localhost,1433;Database=EDCL;User Id=sa;Password=Password123!;TrustServerCertificate=true;";
using var conn = new SqlConnection(connStr);
var countSync = conn.QuerySingle<int>("SELECT COUNT(*) FROM ingestion.sync_sessions");
var countPickup = conn.QuerySingle<int>("SELECT COUNT(*) FROM job.pickup_orders");
Console.WriteLine($"SyncSessions: {countSync}, PickupOrders: {countPickup}");
