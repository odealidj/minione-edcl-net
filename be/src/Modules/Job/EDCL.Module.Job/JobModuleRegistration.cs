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

        return services;
    }

    public static async Task ApplyJobMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        await context.Database.MigrateAsync();
    }
}
