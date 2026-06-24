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

        // Register dummy ports to pass DI validation
        services.AddSingleton<EDCL.Module.Job.Application.Ports.IJobNotificationPort, DummyJobNotificationPort>();
        services.AddSingleton<EDCL.Shared.Kernel.Ports.ITruckPort, DummyTruckPort>();
        services.AddSingleton<EDCL.Shared.Kernel.Ports.ISupplierPort, DummySupplierPort>();

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

internal sealed class DummyTruckPort : EDCL.Shared.Kernel.Ports.ITruckPort
{
    public Task<EDCL.Shared.Kernel.Ports.TruckInfo?> GetTruckByIdAsync(long truckId, CancellationToken ct = default) => Task.FromResult<EDCL.Shared.Kernel.Ports.TruckInfo?>(null);
}

internal sealed class DummySupplierPort : EDCL.Shared.Kernel.Ports.ISupplierPort
{
    public Task<EDCL.Shared.Kernel.Ports.SupplierInfo?> GetSupplierByIdAsync(long supplierId, CancellationToken ct = default) => Task.FromResult<EDCL.Shared.Kernel.Ports.SupplierInfo?>(null);
    public Task<EDCL.Shared.Kernel.Ports.SupplierInfo?> GetSupplierByCodeAsync(string supplierCode, CancellationToken ct = default) => Task.FromResult<EDCL.Shared.Kernel.Ports.SupplierInfo?>(null);
}
