using EDCL.Shared.Infrastructure.Cache;
using EDCL.Shared.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EDCL.Shared.Infrastructure;

public static class SharedInfrastructureRegistration
{
    /// <summary>
    /// Registers all shared infrastructure services.
    /// Must be called BEFORE AddHttpContextAccessor / before modules.
    /// </summary>
    public static IServiceCollection AddEdclSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Redis Sentinel ────────────────────────────────────────────────
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConfig = configuration.GetSection("Redis");
            var useSentinel = (redisConfig["UseSentinel"] ?? "false").ToLower() == "true";
            var password    = redisConfig["Password"];

            var options = new ConfigurationOptions
            {
                ConnectTimeout     = int.Parse(redisConfig["ConnectTimeout"] ?? "5000"),
                SyncTimeout        = int.Parse(redisConfig["SyncTimeout"]    ?? "5000"),
                AbortOnConnectFail = false,
                Password            = password
            };

            if (useSentinel)
            {
                options.ServiceName = redisConfig["MasterName"] ?? "edcl-master";
                options.CommandMap  = CommandMap.Sentinel;
                var endpoints = redisConfig.GetSection("SentinelEndpoints").Get<string[]>() ?? ["localhost:26379"];
                foreach (var ep in endpoints)
                    options.EndPoints.Add(ep);
                return ConnectionMultiplexer.SentinelConnect(options);
            }
            
            options.EndPoints.Add(redisConfig["Endpoint"] ?? "localhost:6379");
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<ICachePort, RedisCacheAdapter>();

        // ── EF Core Interceptors ──────────────────────────────────────────
        services.AddScoped<AuditSaveChangesInterceptor>();

        return services;
    }

    /// <summary>
    /// Registers the CurrentUserService using IHttpContextAccessor (must be called
    /// from a project that references Microsoft.AspNetCore, e.g. EDCL.Api).
    /// Separated from the main registration to keep Shared.Infrastructure
    /// free of ASP.NET Core references.
    /// </summary>
    public static IServiceCollection AddEdclCurrentUser(
        this IServiceCollection services)
    {
        // NOTE: Caller must have called services.AddHttpContextAccessor() first.
        services.AddScoped<ICurrentUserService>(sp =>
        {
            // Resolve IHttpContextAccessor at runtime via dynamic late-binding
            var accessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            return new CurrentUserService(
                getDriverId: () =>
                {
                    var claim = accessor?.HttpContext?.User.FindFirst("sub")?.Value
                             ?? accessor?.HttpContext?.User.FindFirst("driver_id")?.Value;
                    return long.TryParse(claim, out var id) ? id : null;
                },
                getTraceId: () => accessor?.HttpContext?.Items["TraceId"]?.ToString(),
                getIsAuthenticated: () =>
                    accessor?.HttpContext?.User.Identity?.IsAuthenticated ?? false);
        });

        return services;
    }
}
