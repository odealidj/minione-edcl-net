using EDCL.Module.Driver.Domain.Entities;
using EDCL.Worker.GpsTracker.Models;

namespace EDCL.Worker.GpsTracker.Adapters;

public interface IGpsVendorAdapter
{
    Task<List<NormalizedGpsPoint>> GetLatestLocationsAsync(GpsVendor config, CancellationToken cancellationToken);
}
