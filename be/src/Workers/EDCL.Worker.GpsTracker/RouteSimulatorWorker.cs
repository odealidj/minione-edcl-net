using EDCL.Module.Driver.Domain.Entities;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Worker.GpsTracker.Services;
using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker;

public sealed class RouteSimulatorWorker(
    IServiceProvider serviceProvider,
    SimulationSessionManager sessionManager,
    ILogger<RouteSimulatorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RouteSimulatorWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SimulateAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during route simulation.");
            }

            // Run every 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task SimulateAsync(CancellationToken cancellationToken)
    {
        var activeSessions = sessionManager.GetActiveSessions().ToList();
        if (!activeSessions.Any()) return;

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DriverDbContext>();
        var geofenceService = scope.ServiceProvider.GetRequiredService<GeofenceService>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var newLocations = new List<TruckLocation>();
        var endedSessions = new List<long>();

        foreach (var session in activeSessions)
        {
            var point = session.GetCurrentPoint();
            if (point == null)
            {
                endedSessions.Add(session.TruckId);
                continue;
            }

            var lat = point[1];
            var lon = point[0];

            // 1. Save to DB
            var entity = TruckLocation.Create(
                session.TruckId,
                lat,
                lon,
                speed: 40, // Simulated speed
                heading: 0,
                timestamp: DateTime.UtcNow,
                providerName: "Simulator"
            );
            newLocations.Add(entity);

            // 2. Broadcast to UI via SignalR
            var evt = new TruckLocationUpdatedIntegrationEvent
            {
                TruckId = session.TruckId,
                Latitude = lat,
                Longitude = lon,
                Speed = 40,
                Heading = 0,
                Timestamp = DateTime.UtcNow,
                ProviderName = "Simulator"
            };
            await publishEndpoint.Publish(evt, cancellationToken);

            // 3. Geofence evaluation (skip for purely visual interpolated demo routes)
            if (!session.IsInterpolated && session.JobId > 0)
            {
                await geofenceService.EvaluateLocationAsync(session.JobId, lat, lon, session.DestinationLat, session.DestinationLon, cancellationToken);
            }
            
            // Check if reached destination exactly in polyline (last point)
            if (session.CurrentIndex >= session.Polyline.Count - 1)
            {
                endedSessions.Add(session.TruckId);
            }
            else
            {
                session.Advance();
            }
        }

        if (newLocations.Any())
        {
            await dbContext.Set<TruckLocation>().AddRangeAsync(newLocations, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var truckId in endedSessions)
        {
            sessionManager.EndSimulation(truckId);
        }
    }
}
