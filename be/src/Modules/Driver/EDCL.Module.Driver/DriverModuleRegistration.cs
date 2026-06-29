using EDCL.Module.Driver.Infrastructure.Adapters;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EDCL.Module.Driver;

public static class DriverModuleRegistration
{
    public static IServiceCollection AddEdclModuleDriver(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DbContext (schema: [driver]) ──────────────────────────────────
        services.AddDbContext<DriverDbContext>((sp, opt) =>
        {
            opt.UseSqlServer(
                configuration.GetConnectionString("EdclDb"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "driver")
                          .EnableRetryOnFailure(3));
        });

        // ── Cross-domain Port Adapters ────────────────────────────────────
        // These replace the DummySupplierPort and DummyTruckPort in JobModuleRegistration
        services.AddScoped<ISupplierPort, SupplierPortAdapter>();
        services.AddScoped<ITruckPort, TruckPortAdapter>();
        // ── CQRS (MediatR) ────────────────────────────────────────────────
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DriverModuleRegistration).Assembly));

        return services;
    }

    /// <summary>Apply EF Core migrations for the Driver module on startup.</summary>
    public static async Task ApplyDriverMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DriverDbContext>();
        await db.Database.MigrateAsync();
    }
}
