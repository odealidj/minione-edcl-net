using EDCL.Module.Driver.Domain.Entities;
using EDCL.Module.Driver.Application.Jobs;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Worker.GpsTracker.Services;
using EDCL.Worker.GpsTracker.Adapters;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Jobs;

public class GpsSyncJob(
    DriverDbContext dbContext,
    IGpsAdapterFactory adapterFactory,
    SimulationSessionManager sessionManager,
    IConfiguration configuration,
    ILogger<GpsSyncJob> logger) : IGpsSyncJob
{
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(long logisticPartnerId, long gpsVendorId, CancellationToken cancellationToken)
    {
        var mapping = await dbContext.LogisticPartnerGpsVendors
            .Include(m => m.LogisticPartner)
            .Include(m => m.GpsVendor)
            .FirstOrDefaultAsync(m => m.LogisticPartnerId == logisticPartnerId && m.GpsVendorId == gpsVendorId, cancellationToken);

        if (mapping == null || mapping.LogisticPartner.IsDeleted || mapping.GpsVendor.IsDeleted)
        {
            logger.LogWarning("Mapping for Partner {PartnerId} and Vendor {VendorId} not found or is soft-deleted.", logisticPartnerId, gpsVendorId);
            return;
        }

        var partner = mapping.LogisticPartner;
        var vendor = mapping.GpsVendor;

        var adapter = adapterFactory.CreateAdapter(vendor.ProviderType);
        if (adapter == null)
        {
            logger.LogWarning("No adapter found for provider type {ProviderType}", vendor.ProviderType);
            return;
        }

        try
        {
            var locations = await adapter.GetLatestLocationsAsync(vendor, cancellationToken);
            logger.LogInformation("Fetched {Count} locations from {Provider} for partner {PartnerCode}", locations.Count, vendor.ProviderType, partner.Code);

            if (locations.Any())
            {
                var vehicleIds = locations.Select(l => l.GpsVehicleId).Distinct().ToList();
                var trucks = await dbContext.Set<Truck>()
                    .Where(t => t.LogisticPartnerId == partner.Id && t.GpsVehicleId != null && vehicleIds.Contains(t.GpsVehicleId))
                    .ToListAsync(cancellationToken);

                var isAutoAdoptEnabled = configuration.GetValue<bool>("GpsTracker:EnableAutoAdopt", false);
                if (isAutoAdoptEnabled)
                {
                    var existingAssignedIds = trucks.Select(t => t.GpsVehicleId!).ToHashSet();
                    var idsToAdopt = vehicleIds.Where(id => !existingAssignedIds.Contains(id)).ToList();

                    if (idsToAdopt.Any())
                    {
                        var unassignedTrucks = await dbContext.Set<Truck>()
                            .Where(t => t.LogisticPartnerId == partner.Id && (string.IsNullOrEmpty(t.GpsVehicleId) || t.GpsVehicleId.StartsWith("TRK-")))
                            .Take(idsToAdopt.Count)
                            .ToListAsync(cancellationToken);

                        int adoptedCount = 0;
                        for (int i = 0; i < unassignedTrucks.Count && i < idsToAdopt.Count; i++)
                        {
                            unassignedTrucks[i].ConfigureGps(idsToAdopt[i], false); // Adopt and set IsSimulated to false so they animate
                            trucks.Add(unassignedTrucks[i]); // Add to the list so interpolation picks it up
                            adoptedCount++;
                        }

                        if (adoptedCount > 0)
                        {
                            logger.LogInformation("Auto-Adopted {Count} trucks for partner {PartnerCode}", adoptedCount, partner.Code);
                            await dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }
                }

                var newLocations = new List<TruckLocation>();
                var random = new Random();

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
                            var randomOffsetLat = (random.NextDouble() - 0.5) * 0.1;
                            var randomOffsetLon = (random.NextDouble() - 0.5) * 0.1;
                            var destLat = loc.Latitude + randomOffsetLat;
                            var destLon = loc.Longitude + randomOffsetLon;
                            
                            await sessionManager.UpdateOrStartInterpolationSessionAsync(
                                truck.Id,
                                truck.GpsVehicleId!,
                                0, // jobId = 0 denotes dummy job
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
            
            mapping.UpdateSyncStatus(DateTime.UtcNow, "Success", "Successfully synced locations.");
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error pulling data for partner {Partner} from {Vendor}", partner.Code, vendor.ProviderType);
            mapping.UpdateSyncStatus(DateTime.UtcNow, "Failed", ex.Message);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            // Re-throw to trigger Hangfire retry
            throw;
        }
    }
}
