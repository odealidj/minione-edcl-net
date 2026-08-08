namespace EDCL.Module.Notification;

using EDCL.Module.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EDCL.Module.Notification.Infrastructure;

public static class NotificationModuleRegistration
{
    public static IServiceCollection AddEdclModuleNotification(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(NotificationModuleRegistration).Assembly));
        services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();
        services.AddScoped<EDCL.Shared.Kernel.Ports.INotificationPort, EDCL.Module.Notification.Infrastructure.Adapters.NotificationPortAdapter>();

        // Initialize Firebase
        var credentialPath = configuration["Firebase:CredentialPath"];
        if (!string.IsNullOrEmpty(credentialPath) && System.IO.File.Exists(credentialPath))
        {
            if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
                FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
                {
                    Credential = Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault()
                });
            }
        }
        else
        {
            // Fallback or warning if we want to run without firebase in local
            Console.WriteLine("[Warning] Firebase CredentialPath is not configured or file missing. Push notifications will fail.");
        }

        return services;
    }

    public static async Task ApplyNotificationMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await db.Database.MigrateAsync();
    }
}
