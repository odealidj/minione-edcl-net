using System.Net;
using System.Net.Http.Json;
using EDCL.IntegrationTests.Infrastructure;
using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Http.Responses;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Module.Driver.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EDCL.E2ETests;

[Collection("IntegrationTestCollection")]
public class DriverJourney_E2ETest : BaseIntegrationTest
{
    public DriverJourney_E2ETest(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    // Helper records for deserializing standard E2E API responses
    private record LoginResponseDto(string AccessToken);

    [Fact]
    public async Task FullDriverJourney_FromLoginToEndJob_Succeeds()
    {
        // ---------------------------------------------------------------------------------------------------------
        // PHASE 0: SETUP SEED DATA
        // ---------------------------------------------------------------------------------------------------------
        var adminEmail = "e2e_admin@edcl.com";
        var driverPhone = "08119999999";
        var driverPin = "654321";
        long driverId = 0;
        long pickupOrderId = 0;
        long supplierId = 0;
        long stopId = 0;
        long manifestId = 0;
        string kanbanCode = "E2E-PART-001";

        // Seed Auth Data
        await ExecuteInScopeAsync(async sp =>
        {
            var authDb = sp.GetRequiredService<AuthDbContext>();

            // Seed Admin
            var role = await authDb.Roles.FirstOrDefaultAsync(r => r.Code == "ADM") 
                       ?? Role.Create("Administrator", "ADM");
            if (role.Id == 0) { authDb.Roles.Add(role); await authDb.SaveChangesAsync(); }
            
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            var admin = AppUser.Create("E2E Admin", adminEmail, passwordHash, role.Id);
            authDb.AppUsers.Add(admin);
            
            // Seed Driver
            var driverHash = BCrypt.Net.BCrypt.HashPassword(driverPin);
            var driver = Driver.Create("E2E Driver", "E2E-NIK", driverPhone, driverHash);
            driver.ChangePin(driverHash); // Bypass MustChangePin = true
            authDb.Drivers.Add(driver);

            await authDb.SaveChangesAsync();
            driverId = driver.Id;
        });

        // Seed Supplier
        await ExecuteInScopeAsync(async sp =>
        {
            var driverDb = sp.GetRequiredService<DriverDbContext>();
            var supplier = Supplier.Create("SUP-E2E-01", "E2E Supplier", "Address", -6.2, 106.8, 1000);
            driverDb.Suppliers.Add(supplier);
            await driverDb.SaveChangesAsync();
            supplierId = supplier.Id;
        });

        // Seed Job Data
        await ExecuteInScopeAsync(async sp =>
        {
            var jobDb = sp.GetRequiredService<JobDbContext>();

            var po = PickupOrder.Create(driverId, null, "PO-E2E-001", DateTime.UtcNow.AddDays(1), "ROUTE-E2E", "CYC-E2E", TimeSpan.FromHours(8));
            jobDb.PickupOrders.Add(po);
            await jobDb.SaveChangesAsync();
            pickupOrderId = po.Id;

            var stop = PickupOrderDetail.Create(po.Id, supplierId, 1);
            jobDb.PickupOrderDetails.Add(stop);
            await jobDb.SaveChangesAsync();
            stopId = stop.Id;

            var manifest = PickupOrderManifest.Create(stop.Id, "MAN-E2E-001", 1);
            jobDb.PickupOrderManifests.Add(manifest);
            await jobDb.SaveChangesAsync();
            manifestId = manifest.Id;
        });

        // ---------------------------------------------------------------------------------------------------------
        // PHASE 1: ADMIN LOGIN
        // ---------------------------------------------------------------------------------------------------------
        var adminLoginCmd = new LoginAppUserCommand(adminEmail, "Admin123!");
        var adminLoginRes = await Client.PostAsJsonAsync("/api/v1/auth/admin/login", adminLoginCmd);
        adminLoginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminTokenResult = await DeserializeResponseAsync<LoginResponseDto>(adminLoginRes);
        adminTokenResult!.Status.Should().Be("success");
        var adminToken = adminTokenResult.Data!.AccessToken;

        // ---------------------------------------------------------------------------------------------------------
        // PHASE 2: DRIVER LOGIN
        // ---------------------------------------------------------------------------------------------------------
        var driverLoginPayload = new { phoneNumber = driverPhone, pin = driverPin };
        var driverLoginRes = await Client.PostAsJsonAsync("/api/v1/auth/drivers/login", driverLoginPayload);
        driverLoginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var driverTokenResult = await DeserializeResponseAsync<LoginResponseDto>(driverLoginRes);
        driverTokenResult!.Status.Should().Be("success");
        
        var jwtToken = driverTokenResult.Data!.AccessToken;
        AuthenticateClient(jwtToken);

        // ---------------------------------------------------------------------------------------------------------
        // PHASE 3: GET DASHBOARD
        // ---------------------------------------------------------------------------------------------------------
        var dashboardRes = await Client.GetAsync("/api/v1/jobs/dashboard");
        dashboardRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // ---------------------------------------------------------------------------------------------------------
        // PHASE 4: START JOB
        // ---------------------------------------------------------------------------------------------------------
        Client.DefaultRequestHeaders.Remove("X-Idempotency-Key");
        Client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        // truckId is nullable in start command
        var startJobPayload = new { pickupOrderId = pickupOrderId, truckId = (long?)null };
        var startJobRes = await Client.PostAsJsonAsync($"/api/v1/jobs/{pickupOrderId}/start", startJobPayload);
        if (startJobRes.StatusCode != HttpStatusCode.OK)
        {
             var err = await startJobRes.Content.ReadAsStringAsync();
             throw new Exception($"Start Job failed: {err}");
        }
        startJobRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // ---------------------------------------------------------------------------------------------------------
        // PHASE 5: SCAN KANBAN
        // ---------------------------------------------------------------------------------------------------------
        Client.DefaultRequestHeaders.Remove("X-Idempotency-Key");
        Client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        var scanPayload = new { kanbanCode = kanbanCode };
        var scanRes = await Client.PostAsJsonAsync($"/api/v1/jobs/stops/{stopId}/manifests/{manifestId}/kanban", scanPayload);
        if (scanRes.StatusCode != HttpStatusCode.OK)
        {
             var err = await scanRes.Content.ReadAsStringAsync();
             throw new Exception($"Scan Kanban failed: {err}");
        }
        scanRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // ---------------------------------------------------------------------------------------------------------
        // PHASE 6: COMPLETE STOP
        // ---------------------------------------------------------------------------------------------------------
        Client.DefaultRequestHeaders.Remove("X-Idempotency-Key");
        Client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        var completeStopPayload = new { latitude = -6.2, longitude = 106.8 };
        var completeStopRes = await Client.PostAsJsonAsync($"/api/v1/jobs/stops/{stopId}/complete", completeStopPayload);
        if (completeStopRes.StatusCode != HttpStatusCode.OK)
        {
             var err = await completeStopRes.Content.ReadAsStringAsync();
             throw new Exception($"Complete stop failed: {err}");
        }
        completeStopRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // ---------------------------------------------------------------------------------------------------------
        // PHASE 7: END JOB
        // ---------------------------------------------------------------------------------------------------------
        Client.DefaultRequestHeaders.Remove("X-Idempotency-Key");
        Client.DefaultRequestHeaders.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        var endJobRes = await Client.PostAsJsonAsync($"/api/v1/jobs/{pickupOrderId}/end", new { });
        if (endJobRes.StatusCode != HttpStatusCode.OK)
        {
             var err = await endJobRes.Content.ReadAsStringAsync();
             throw new Exception($"End job failed: {err}");
        }
        endJobRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
