using System.Net.Http.Headers;
using System.Text.Json;
using EDCL.Shared.Kernel.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MessagePack;
using EDCL.Shared.Http.Middlewares;
using EDCL.Shared.Http.Responses;

namespace EDCL.IntegrationTests.Infrastructure;

[CollectionDefinition("IntegrationTestCollection")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[Collection("IntegrationTestCollection")]
public abstract class BaseIntegrationTest
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Utility to get a scoped DbContext to seed data directly.
    /// Note: You need to specify the specific module's DbContext type if you have multiple.
    /// Since the architecture uses multiple DbContexts, pass the correct one.
    /// </summary>
    protected async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Utility to execute inside a scope and return a result.
    /// </summary>
    protected async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Injects a Bearer token into the HttpClient for authenticated requests.
    /// </summary>
    protected void AuthenticateClient(string token)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Deserializes JSON response from API.
    /// </summary>
    protected async Task<ApiResponse<T>?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<T>>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
    }
}
