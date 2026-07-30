using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Worker.GpsTracker.Services;
using EDCL.Module.Driver.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker;

public sealed class RealGpsPollingWorker(
    IServiceProvider serviceProvider,
    IGpsAdapterFactory adapterFactory,
    ILogger<RealGpsPollingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RealGpsPollingWorker started.");

        var random = new Random();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollGpsDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while polling GPS data.");
            }

            // Randomized Polling: Run every 5 to 20 minutes
            var nextDelayMinutes = random.Next(5, 21);
            logger.LogInformation("Next GPS polling in {Minutes} minutes.", nextDelayMinutes);
            await Task.Delay(TimeSpan.FromMinutes(nextDelayMinutes), stoppingToken);
        }
    }

    private async Task PollGpsDataAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DriverDbContext>();
        var sessionManager = scope.ServiceProvider.GetRequiredService<SimulationSessionManager>();

        var mappings = await dbContext.Set<LogisticPartnerGpsVendor>()
            .Include(m => m.LogisticPartner)
            .Include(m => m.GpsVendor)
            .Where(m => !m.LogisticPartner.IsDeleted && !m.GpsVendor.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!mappings.Any())
        {
            logger.LogInformation("No active GPS vendor mappings found.");
            return;
        }

        foreach (var mapping in mappings)
        {
            var partner = mapping.LogisticPartner;
            var vendor = mapping.GpsVendor;

            var adapter = adapterFactory.CreateAdapter(vendor.ProviderType);
            if (adapter == null)
            {
                logger.LogWarning("No adapter found for provider type {ProviderType}", vendor.ProviderType);
                continue;
            }

            try
            {
                var locations = await adapter.GetLatestLocationsAsync(vendor, cancellationToken);
                logger.LogInformation("Fetched {Count} locations from {Provider} for partner {PartnerCode}", locations.Count, vendor.ProviderType, partner.Code);

                if (locations.Any())
                {
                    // Map to actual Truck entities in our DB
                    var vehicleIds = locations.Select(l => l.GpsVehicleId).ToList();
                    var trucks = await dbContext.Set<Truck>()
                        .Where(t => t.LogisticPartnerId == partner.Id && t.GpsVehicleId != null && vehicleIds.Contains(t.GpsVehicleId))
                        .ToListAsync(cancellationToken);

                    var newLocations = new List<TruckLocation>();
                    foreach (var loc in locations)
                    {
                        var truck = trucks.FirstOrDefault(t => t.GpsVehicleId == loc.GpsVehicleId);
                        if (truck != null)
                        {
                            var entity = TruckLocation.Create(
                                truck.Id,
                                loc.Latitude,
                                loc.Longitude,
                                loc.Speed,
                                loc.Heading,
                                loc.Timestamp,
                                loc.ProviderName);
                            
                            newLocations.Add(entity);

                            // Trigger Route Interpolation (Predictive Movement) for demo
                            if (!truck.IsSimulated)
                            {
                                // Dummy destination: randomly shift latitude/longitude by ~5km to give a movement illusion
                                // 0.05 degrees is roughly 5.5 km
                                var randomOffsetLat = (new Random().NextDouble() - 0.5) * 0.1;
                                var randomOffsetLon = (new Random().NextDouble() - 0.5) * 0.1;
                                var destLat = loc.Latitude + randomOffsetLat;
                                var destLon = loc.Longitude + randomOffsetLon;
                                
                                // We pass jobId = 0 to denote it's an interpolated dummy job
                                await sessionManager.UpdateOrStartInterpolationSessionAsync(
                                    truck.Id,
                                    truck.GpsVehicleId!,
                                    0,
                                    loc.Latitude,
                                    loc.Longitude,
                                    destLat,
                                    destLon,
                                    cancellationToken);
                            }
                        }
                    }

                    if (newLocations.Any())
                    {
                        await dbContext.Set<TruckLocation>().AddRangeAsync(newLocations, cancellationToken);
                    }
                }
                
                // Update Partner Sync Status
                mapping.UpdateSyncStatus(DateTime.UtcNow, "Success", "Successfully synced locations.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error pulling data for partner {Partner} from {Vendor}", partner.Code, vendor.ProviderType);
                mapping.UpdateSyncStatus(DateTime.UtcNow, "Failed", ex.Message);
            }
        }

        // Save status and any new locations
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
