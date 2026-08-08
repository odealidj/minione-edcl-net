using System.Collections.Concurrent;
using EDCL.Shared.Kernel.Events;
using EDCL.Worker.GpsTracker.Models;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Services;

public class SimulationSessionManager(OsrmClient osrmClient, ILogger<SimulationSessionManager> logger)
{
    // Key: TruckId
    private readonly ConcurrentDictionary<long, ActiveSimulationSession> _activeSessions = new();

    public async Task StartSimulationAsync(SimulationStartedIntegrationEvent evt, CancellationToken cancellationToken)
    {
        var polyline = await osrmClient.GetRoutePolylineAsync(evt.StartLat, evt.StartLon, evt.EndLat, evt.EndLon, cancellationToken);
        if (polyline == null || polyline.Count == 0)
        {
            logger.LogWarning("Failed to get polyline for simulation Job {JobId} Truck {TruckId}", evt.JobId, evt.TruckId);
            return;
        }

        var session = new ActiveSimulationSession
        {
            TruckId = evt.TruckId,
            GpsVehicleId = evt.GpsVehicleId,
            JobId = evt.JobId,
            Polyline = polyline,
            CurrentIndex = 0,
            DestinationLat = evt.EndLat,
            DestinationLon = evt.EndLon
        };

        _activeSessions[evt.TruckId] = session;
        logger.LogInformation("Started simulation for Truck {TruckId} on Job {JobId} with {Points} points.", evt.TruckId, evt.JobId, polyline.Count);
    }

    public async Task UpdateOrStartInterpolationSessionAsync(long truckId, string gpsVehicleId, long jobId, double startLat, double startLon, double endLat, double endLon, CancellationToken cancellationToken)
    {
        var polyline = await osrmClient.GetRoutePolylineAsync(startLat, startLon, endLat, endLon, cancellationToken);
        if (polyline == null || polyline.Count == 0)
        {
            logger.LogWarning("Failed to get interpolation polyline for Truck {TruckId}", truckId);
            return;
        }

        var session = new ActiveSimulationSession
        {
            TruckId = truckId,
            GpsVehicleId = gpsVehicleId,
            JobId = jobId,
            Polyline = polyline,
            CurrentIndex = 0,
            DestinationLat = endLat,
            DestinationLon = endLon,
            IsInterpolated = true // mark as interpolated so we know it's a real truck
        };

        _activeSessions[truckId] = session;
        logger.LogInformation("Started/Updated interpolation for Truck {TruckId} with {Points} points.", truckId, polyline.Count);
    }

    public void EndSimulation(long truckId)
    {
        if (_activeSessions.TryRemove(truckId, out var session))
        {
            logger.LogInformation("Ended simulation for Truck {TruckId}", truckId);
        }
    }

    public void ClearSessions()
    {
        _activeSessions.Clear();
        logger.LogInformation("All active simulation sessions have been cleared.");
    }

    public IEnumerable<ActiveSimulationSession> GetActiveSessions()
    {
        return _activeSessions.Values;
    }
}

public class ActiveSimulationSession
{
    public long TruckId { get; set; }
    public string GpsVehicleId { get; set; } = default!;
    public long JobId { get; set; }
    
    // Polyline points [lon, lat]
    public List<double[]> Polyline { get; set; } = [];
    public int CurrentIndex { get; set; }
    
    public double DestinationLat { get; set; }
    public double DestinationLon { get; set; }
    
    public bool IsInterpolated { get; set; } = false;
    
    public double[]? GetCurrentPoint()
    {
        if (CurrentIndex < Polyline.Count)
        {
            return Polyline[CurrentIndex];
        }
        return null;
    }
    
    public void Advance()
    {
        if (CurrentIndex < Polyline.Count - 1)
        {
            CurrentIndex++;
        }
    }
}
