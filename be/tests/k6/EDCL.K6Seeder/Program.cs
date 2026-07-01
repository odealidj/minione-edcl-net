using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Module.Driver.Domain.Entities;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Infrastructure.Persistence;
using BCrypt.Net;

namespace EDCL.K6Seeder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("EDCL K6 Performance Test Data Seeder");
            
            // Build configuration to optionally read from appsettings or use defaults
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection") 
                ?? "Server=127.0.0.1,1444;Database=edcl;User Id=sa;Password=EdclMini_123!;TrustServerCertificate=true;Encrypt=false;";

            Console.WriteLine($"Using DB: {connectionString}");

            var authOptions = new DbContextOptionsBuilder<AuthDbContext>().UseSqlServer(connectionString).Options;
            var jobOptions = new DbContextOptionsBuilder<JobDbContext>().UseSqlServer(connectionString).Options;
            var driverOptions = new DbContextOptionsBuilder<DriverDbContext>().UseSqlServer(connectionString).Options;

            var interceptor = new AuditSaveChangesInterceptor(new SystemUserService());

            using var authDb = new AuthDbContext(authOptions, interceptor);
            using var jobDb = new JobDbContext(jobOptions, interceptor);
            using var driverDb = new DriverDbContext(driverOptions, interceptor);

            Console.WriteLine("Connecting to Database...");
            // Ensure connection
            await authDb.Database.CanConnectAsync();

            if (args.Contains("clean", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cleaning up K6 Seed Data...");
                await jobDb.Database.ExecuteSqlRawAsync("DELETE FROM job.pickup_order_kanbans WHERE PickupOrderManifestId IN (SELECT Id FROM job.pickup_order_manifests WHERE ManifestNo LIKE 'MAN-K6-%')");
                await jobDb.Database.ExecuteSqlRawAsync("DELETE FROM job.pickup_order_manifests WHERE ManifestNo LIKE 'MAN-K6-%'");
                await jobDb.Database.ExecuteSqlRawAsync("DELETE FROM job.pickup_order_details WHERE PickupOrderId IN (SELECT Id FROM job.pickup_orders WHERE delivery_no LIKE 'PO-K6-%')");
                await jobDb.Database.ExecuteSqlRawAsync("DELETE FROM job.pickup_orders WHERE delivery_no LIKE 'PO-K6-%'");
                
                await driverDb.Database.ExecuteSqlRawAsync("DELETE FROM driver.suppliers WHERE SupplierCode = 'SUP-K6'");
                
                await authDb.Database.ExecuteSqlRawAsync("DELETE FROM auth.drivers WHERE Nik LIKE 'NIK-K6-%'");
                
                var jsonPathDelete = Path.Combine(Directory.GetCurrentDirectory(), "..", "users.json");
                if (File.Exists(jsonPathDelete)) File.Delete(jsonPathDelete);
                
                Console.WriteLine("Cleanup Complete!");
                return;
            }

            int targetVUs = 100; // How many concurrent drivers you want to simulate
            Console.WriteLine($"Seeding {targetVUs} Virtual Users (Drivers) and Jobs...");

            var usersData = new List<object>();
            
            // 1. Seed a generic Supplier for Geofence
            var supplier = await driverDb.Suppliers.FirstOrDefaultAsync(s => s.SupplierCode == "SUP-K6");
            if (supplier == null)
            {
                supplier = Supplier.Create("SUP-K6", "K6 Supplier", "K6 Address", -6.2, 106.8, 1000);
                driverDb.Suppliers.Add(supplier);
                await driverDb.SaveChangesAsync();
            }

            var basePhone = 8200000000;
            var driverPin = "123456";
            var driverHash = BCrypt.Net.BCrypt.HashPassword(driverPin, 9);
            
            for (int i = 1; i <= targetVUs; i++)
            {
                var phone = $"0{basePhone + i}";
                var poNo = $"PO-K6-V3-{i}";
                var kanbanCode = $"KB-K6-V3-{i}";

                // 2. Auth Driver
                var driver = await authDb.Drivers.FirstOrDefaultAsync(d => d.PhoneNumber == phone);
                if (driver == null)
                {
                    driver = Driver.Create($"K6 Driver V3 {i}", $"NIK-K6-V3-{i}", phone, driverHash);
                    driver.ChangePin(driverHash); // Bypass force change pin
                    authDb.Drivers.Add(driver);
                    await authDb.SaveChangesAsync();
                }

                // 3. Job PickupOrder
                var po = await jobDb.PickupOrders.FirstOrDefaultAsync(p => p.PoNo == poNo);
                if (po == null)
                {
                    po = PickupOrder.Create(driver.Id, null, poNo, DateTime.UtcNow.AddDays(1), "RT-K6", "CYC-K6", TimeSpan.FromHours(8));
                    jobDb.PickupOrders.Add(po);
                    await jobDb.SaveChangesAsync(); // Save to get PO ID
                }
                else
                {
                    // Clean up status if rerunning
                    await jobDb.Database.ExecuteSqlRawAsync($"UPDATE job.pickup_orders SET Status = 'PENDING' WHERE Id = {po.Id}");
                }

                // 4. Job PickupOrderDetail (Stop)
                var stop = await jobDb.PickupOrderDetails.FirstOrDefaultAsync(s => s.PickupOrderId == po.Id);
                if (stop == null)
                {
                    stop = PickupOrderDetail.Create(po.Id, supplier.Id, 1);
                    jobDb.PickupOrderDetails.Add(stop);
                    await jobDb.SaveChangesAsync();
                }
                else
                {
                    await jobDb.Database.ExecuteSqlRawAsync($"UPDATE job.pickup_order_details SET Status = 'PENDING' WHERE Id = {stop.Id}");
                }

                // 5. Manifest
                var manifest = await jobDb.PickupOrderManifests.FirstOrDefaultAsync(m => m.PickupOrderDetailId == stop.Id);
                if (manifest == null)
                {
                    manifest = PickupOrderManifest.Create(stop.Id, $"MAN-K6-V3-{i}", 1);
                    jobDb.PickupOrderManifests.Add(manifest);
                    await jobDb.SaveChangesAsync();
                }
                else
                {
                    await jobDb.Database.ExecuteSqlRawAsync($"UPDATE job.pickup_order_manifests SET Status = 'PENDING', ScannedKanban = 0 WHERE Id = {manifest.Id}");
                }
                
                // Clear any existing scanned kanbans for this manifest
                await jobDb.Database.ExecuteSqlRawAsync($"DELETE FROM job.pickup_order_kanbans WHERE PickupOrderManifestId = {manifest.Id}");

                usersData.Add(new
                {
                    phone = phone,
                    pin = driverPin,
                    pickupOrderId = po.Id,
                    stopId = stop.Id,
                    manifestId = manifest.Id,
                    kanbanCode = kanbanCode,
                    supplierLat = supplier.Latitude,
                    supplierLng = supplier.Longitude
                });

                if (i % 10 == 0) Console.WriteLine($"Generated {i}/{targetVUs} users...");
            }

            // Export to JSON for k6
            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "users.json");
            var json = JsonSerializer.Serialize(usersData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(jsonPath, json);

            Console.WriteLine($"Successfully seeded {targetVUs} VUs!");
            Console.WriteLine($"Exported credentials to {jsonPath}");
        }
    }
}
