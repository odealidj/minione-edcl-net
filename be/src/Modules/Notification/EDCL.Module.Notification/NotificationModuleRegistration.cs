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
        var credentialPath = configuration["Firebase:CredentialPath"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        string? resolvedPath = null;
        if (!string.IsNullOrEmpty(credentialPath))
        {
            if (Path.IsPathRooted(credentialPath) && File.Exists(credentialPath))
            {
                resolvedPath = credentialPath;
            }
            else
            {
                var candidatePaths = new[]
                {
                    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), credentialPath)),
                    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "be", credentialPath)),
                    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, credentialPath))
                };

                resolvedPath = candidatePaths.FirstOrDefault(File.Exists);
            }
        }

        // Auto-discovery fallback for local development if exact file wasn't matched
        if (string.IsNullOrEmpty(resolvedPath))
        {
            var searchDirs = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "firebase"),
                Path.Combine(Directory.GetCurrentDirectory(), "be", "firebase"),
                Path.Combine(AppContext.BaseDirectory, "firebase")
            };

            foreach (var dir in searchDirs)
            {
                if (Directory.Exists(dir))
                {
                    var file = Directory.GetFiles(dir, "*.json")
                        .FirstOrDefault(f => !f.EndsWith(".example.json", StringComparison.OrdinalIgnoreCase));
                    if (file != null)
                    {
                        resolvedPath = file;
                        break;
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
        {
            if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", resolvedPath);
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
