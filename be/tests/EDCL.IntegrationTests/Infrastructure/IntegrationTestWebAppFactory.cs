using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace EDCL.IntegrationTests.Infrastructure;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _dbContainer;
    private readonly IContainer _redisContainer;
    private readonly IContainer _rabbitMqContainer;

    static IntegrationTestWebAppFactory()
    {
        Environment.SetEnvironmentVariable("DOCKER_HOST", "unix:///run/user/1000/podman/podman.sock");
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }

    public IntegrationTestWebAppFactory()
    {
        // SQL Server Testcontainer (Generic to avoid MsSqlBuilder Exec bug in Podman)
        _dbContainer = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", "StrongPassword123!")
            .WithPortBinding(1433, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server is listening on"))
            .Build();

        // Redis Testcontainer
        _redisContainer = new ContainerBuilder()
            .WithImage("redis:7.2-alpine")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
            .Build();

        // RabbitMQ Testcontainer
        _rabbitMqContainer = new ContainerBuilder()
            .WithImage("rabbitmq:3.13-management-alpine")
            .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
            .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
            .WithPortBinding(5672, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete"))
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var sqlPort = _dbContainer.GetMappedPublicPort(1433);
            var sqlHost = _dbContainer.Hostname;
            var sqlConnString = $"Server={sqlHost},{sqlPort};Database=EdclTestDb;User Id=sa;Password=StrongPassword123!;TrustServerCertificate=True;";

            var redisPort = _redisContainer.GetMappedPublicPort(6379);
            var redisHost = _redisContainer.Hostname;
            var redisConnString = $"{redisHost}:{redisPort}";

            var rmHost = _rabbitMqContainer.Hostname;
            var rmPort = _rabbitMqContainer.GetMappedPublicPort(5672);
            var rmConnString = $"amqp://guest:guest@{rmHost}:{rmPort}";

            var testConfig = new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", sqlConnString },
                { "ConnectionStrings:EdclDb", sqlConnString },
                { "ConnectionStrings:RedisConnection", redisConnString },
                { "ConnectionStrings:RabbitMqConnection", rmConnString }
            };

            config.AddInMemoryCollection(testConfig);
        });
        
        builder.ConfigureTestServices(services =>
        {
            // Override services if needed (e.g. mocked external APIs)
        });
    }

    public async Task InitializeAsync()
    {
        // Increase timeout globally if needed, though usually handled by builder
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _redisContainer.StartAsync(),
            _rabbitMqContainer.StartAsync()
        );
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(
            _dbContainer.DisposeAsync().AsTask(),
            _redisContainer.DisposeAsync().AsTask(),
            _rabbitMqContainer.DisposeAsync().AsTask()
        );
    }
}
