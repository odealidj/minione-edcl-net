using EDCL.Module.Job.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EDCL.Module.Job;

public static class JobModuleRegistration
{
    public static IServiceCollection AddEdclModuleJob(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<JobDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("EdclDb"));
        });

        services.AddScoped<EDCL.Module.Job.Application.Ports.IPickupOrderRepository, EDCL.Module.Job.Infrastructure.Persistence.Repositories.PickupOrderRepository>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(JobModuleRegistration).Assembly));

        // Register dummy notification port (real implementation will come with Outbox Pattern)
        services.AddSingleton<EDCL.Module.Job.Application.Ports.IJobNotificationPort, DummyJobNotificationPort>();

        // NOTE: ISupplierPort and ITruckPort are now registered by DriverModuleRegistration
        // with real database-backed implementations.

        return services;
    }

    public static async Task ApplyJobMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        await context.Database.MigrateAsync();
    }
}

internal sealed class DummyJobNotificationPort : EDCL.Module.Job.Application.Ports.IJobNotificationPort
{
    public Task NotifyDriverJobStartedAsync(long driverId, long pickupOrderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyDriverJobCompletedAsync(long driverId, long pickupOrderId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

