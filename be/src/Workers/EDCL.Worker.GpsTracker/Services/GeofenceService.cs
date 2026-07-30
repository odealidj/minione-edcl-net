using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Services;

public class GeofenceService(IPublishEndpoint publishEndpoint, ILogger<GeofenceService> logger)
{
    private const double DefaultRadiusMeters = 500;

    public async Task EvaluateLocationAsync(long jobId, double truckLat, double truckLon, double destinationLat, double destinationLon, CancellationToken cancellationToken)
    {
        var distance = CalculateDistanceMeters(truckLat, truckLon, destinationLat, destinationLon);
        
        if (distance <= DefaultRadiusMeters)
        {
            logger.LogInformation("Job {JobId} truck reached destination. Distance: {Distance}m", jobId, distance);
            
            // Publish event to mark job as arrived/completed.
            // In a real system, we'd trigger a specific command, but for now we'll publish an event that Cargo/Job modules can listen to.
            var integrationEvent = new ManifestDeliveredIntegrationEvent
            {
                ManifestId = jobId, // In this demo, we assume jobId maps to manifest or we trigger simulate delivery
                ManifestNo = $"SIM-JOB-{jobId}",
                Status = "Delivered",
                DeliveredAt = DateTime.UtcNow,
                Remarks = "Auto-EndJob by Geofence Simulator"
            };

            await publishEndpoint.Publish(integrationEvent, cancellationToken);
            
            // Note: The SimulationSessionManager handles stopping the simulation for this truck. 
            // We could raise an event here to trigger `EndSimulation`.
        }
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3; // Earth radius in meters
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var deltaPhi = (lat2 - lat1) * Math.PI / 180;
        var deltaLambda = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}
