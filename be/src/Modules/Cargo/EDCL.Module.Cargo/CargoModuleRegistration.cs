namespace EDCL.Module.Cargo;

using EDCL.Module.Cargo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class CargoModuleRegistration
{
    public static IServiceCollection AddEdclModuleCargo(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<Infrastructure.Channels.IngestionErrorChannel>();

        services.AddDbContext<CargoDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CargoModuleRegistration).Assembly));

        return services;
    }

    public static async Task ApplyCargoMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CargoDbContext>();
        await db.Database.MigrateAsync();
    }
}
