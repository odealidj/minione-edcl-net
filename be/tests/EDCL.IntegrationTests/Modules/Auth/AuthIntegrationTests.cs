using System.Net;
using System.Net.Http.Json;
using EDCL.IntegrationTests.Infrastructure;
using EDCL.Module.Auth.Application.Commands.Login;
using EDCL.Module.Auth.Domain.Entities;
using EDCL.Module.Auth.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EDCL.IntegrationTests.Modules.Auth;

public class AuthIntegrationTests : BaseIntegrationTest
{
    public AuthIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AdminLogin_WithValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        var testEmail = "admin@edcl.com";
        var testPassword = "Password123!";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(testPassword);

        await ExecuteInScopeAsync(async sp =>
        {
            var dbContext = sp.GetRequiredService<AuthDbContext>();
            
            // Clear existing for clean state (or use transactional tests in real scenarios)
            var existingUser = await dbContext.AppUsers.FirstOrDefaultAsync(u => u.Email == testEmail);
            if (existingUser != null)
            {
                dbContext.AppUsers.Remove(existingUser);
            }
            
            var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Code == "ADM") 
                       ?? Role.Create("Administrator", "ADM");
            
            if (role.Id == 0) 
            {
                dbContext.Roles.Add(role);
                await dbContext.SaveChangesAsync();
            }
            
            var appUser = AppUser.Create("System Admin", testEmail, passwordHash, role.Id);
            dbContext.AppUsers.Add(appUser);
            await dbContext.SaveChangesAsync();
        });

        var command = new LoginAppUserCommand(testEmail, testPassword);

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/web/auth/staff/admin/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await DeserializeResponseAsync<LoginAppUserResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be("success");
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
    }
}
