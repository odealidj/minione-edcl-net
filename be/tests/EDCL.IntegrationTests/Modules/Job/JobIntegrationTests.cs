using System.Net;
using System.Net.Http.Json;
using EDCL.IntegrationTests.Infrastructure;
using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EDCL.IntegrationTests.Modules.Job;

public class JobIntegrationTests : BaseIntegrationTest
{
    public JobIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ScanKanban_WithValidData_ReturnsOkAndUpdatesDb()
    {
        // Arrange
        var testPhone = "08111222333";
        var driverId = 0L;
        var pickupOrderId = 0L;
        var pickupOrderStopId = 0L;
        var manifestId = 0L;
        var jwtToken = string.Empty;

        // 1. Seed Auth for Token
        await ExecuteInScopeAsync(async sp =>
        {
            var authDb = sp.GetRequiredService<AuthDbContext>();
            var jwtService = sp.GetRequiredService<IJwtTokenService>();

            var driver = Driver.Create("Integration Driver", "1234567890123456", testPhone, "dummy_hash");
            authDb.Drivers.Add(driver);
            await authDb.SaveChangesAsync();
            driverId = driver.Id;

            // Generate real JWT token
            var tokenResult = jwtService.GenerateAccessToken(driver);
            jwtToken = tokenResult.AccessToken;
        });

        // 2. Seed Job & Cargo
        await ExecuteInScopeAsync(async sp =>
        {
            var jobDb = sp.GetRequiredService<JobDbContext>();

            var po = PickupOrder.Create(driverId, null, "PO-INT-001", DateTime.UtcNow.AddDays(1), "ROUTE-A", "CYC-1", TimeSpan.FromHours(8));
            jobDb.PickupOrders.Add(po);
            await jobDb.SaveChangesAsync();
            pickupOrderId = po.Id;

            var stop = PickupOrderDetail.Create(po.Id, 1, 1);
            jobDb.PickupOrderDetails.Add(stop);
            await jobDb.SaveChangesAsync();
            pickupOrderStopId = stop.Id;

            var manifest = PickupOrderManifest.Create(stop.Id, "MAN-001", 1);
            jobDb.PickupOrderManifests.Add(manifest);
            await jobDb.SaveChangesAsync();
            manifestId = manifest.Id;

        });

        AuthenticateClient(jwtToken);
        var kanbanPartNo = "PART-001";

        // Act
        // Path: POST /api/v1/mobile/jobs/stops/{stopId}/manifests/{manifestId}/kanban
        var payload = new { kanbanCode = kanbanPartNo };
        var response = await Client.PostAsJsonAsync($"/api/v1/mobile/jobs/stops/{pickupOrderStopId}/manifests/{manifestId}/kanban", payload);

        // Assert API Response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Assert Database State
        await ExecuteInScopeAsync(async sp =>
        {
            var jobDb = sp.GetRequiredService<JobDbContext>();
            var updatedKanban = await jobDb.PickupOrderKanbans
                .FirstOrDefaultAsync(k => k.PickupOrderManifestId == manifestId && k.KanbanCode == kanbanPartNo);
            
            updatedKanban.Should().NotBeNull();
            updatedKanban!.Status.Should().Be("SCANNED");
            updatedKanban.ScannedAt.Should().NotBe(default(DateTime));
        });
    }
}
