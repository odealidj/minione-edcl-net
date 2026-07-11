using System.Net;
using EDCL.IntegrationTests.Infrastructure;
using EDCL.Module.Auth.Application.Ports;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Module.Auth.Infrastructure.Persistence;
using EDCL.Module.Cargo.Domain.Entities;
using EDCL.Module.Cargo.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EDCL.IntegrationTests.Modules.Cargo;

public class CargoIntegrationTests : BaseIntegrationTest
{
    public CargoIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetManifestDetail_WithValidManifest_ReturnsOk()
    {
        // Arrange
        var testPhone = "082233334444";
        var driverId = 0L;
        var jwtToken = string.Empty;
        var manifestNo = "MAN-CARGO-INT-01";
        
        // 1. Seed Auth for Token
        await ExecuteInScopeAsync(async sp =>
        {
            var authDb = sp.GetRequiredService<AuthDbContext>();
            var jwtService = sp.GetRequiredService<IJwtTokenService>();

            var driver = Driver.Create("Integration Cargo Driver", "CARGO-NIK-123", testPhone, "dummy_hash");
            authDb.Drivers.Add(driver);
            await authDb.SaveChangesAsync();
            driverId = driver.Id;

            // Generate real JWT token
            var tokenResult = jwtService.GenerateAccessToken(driver);
            jwtToken = tokenResult.AccessToken;
        });

        // 2. Seed Cargo Data
        await ExecuteInScopeAsync(async sp =>
        {
            var cargoDb = sp.GetRequiredService<CargoDbContext>();

            var manifest = new Manifest(manifestNo, "SUP-01", "Supplier 1", 1, DateTime.UtcNow, "CYC-1", "ORD-1", "DOCK-1", "LANE-1");
            cargoDb.Manifests.Add(manifest);
            await cargoDb.SaveChangesAsync();

            var part = new ManifestPart(manifest.Id, "PART-A-001", "Part A", 10, "KBN-1", "UNIQ-1", "BX1");
            cargoDb.ManifestParts.Add(part);
            await cargoDb.SaveChangesAsync();

            var kanban = new ManifestKanban(manifest.Id, "PART-A-001", "KANBAN-A-001");
            cargoDb.ManifestKanbans.Add(kanban);
            await cargoDb.SaveChangesAsync();
        });

        AuthenticateClient(jwtToken);

        // Act
        // Path: GET /api/v1/mobile/cargo/manifests/{manifestNo}/detail
        var response = await Client.GetAsync($"/api/v1/mobile/cargo/manifests/{manifestNo}/detail");

        // Assert API Response
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
