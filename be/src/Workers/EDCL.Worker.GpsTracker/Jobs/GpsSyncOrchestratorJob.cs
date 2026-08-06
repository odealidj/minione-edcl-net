using EDCL.Module.Driver.Application.Jobs;
using EDCL.Module.Driver.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Jobs;

public class GpsSyncOrchestratorJob(
    DriverDbContext dbContext,
    IBackgroundJobClient backgroundJobClient,
    ILogger<GpsSyncOrchestratorJob> logger) : IGpsSyncOrchestratorJob
{
    private readonly int[] _pollIntervals = { 1, 5, 10, 15, 20 };

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        // Find mappings that need to be synced (NextGpsSyncAt is null or in the past)
        var mappingsToSync = await dbContext.LogisticPartnerGpsVendors
            .Include(m => m.LogisticPartner)
            .Include(m => m.GpsVendor)
            .Where(m => !m.LogisticPartner.IsDeleted && !m.GpsVendor.IsDeleted)
            .Where(m => m.NextGpsSyncAt == null || m.NextGpsSyncAt <= utcNow)
            .ToListAsync(cancellationToken);

        if (!mappingsToSync.Any())
        {
            return;
        }

        var random = new Random();

        foreach (var mapping in mappingsToSync)
        {
            logger.LogInformation("Enqueuing GPS sync job for partner {PartnerCode} and vendor {VendorCode}", 
                mapping.LogisticPartner.Code, mapping.GpsVendor.ProviderType);

            // Enqueue the background job using interface
            backgroundJobClient.Enqueue<IGpsSyncJob>(job => job.ExecuteAsync(mapping.LogisticPartnerId, mapping.GpsVendorId, CancellationToken.None));

            // Calculate next poll time
            var nextDelayMinutes = _pollIntervals[random.Next(_pollIntervals.Length)];
            mapping.SetNextSyncTime(utcNow.AddMinutes(nextDelayMinutes));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
