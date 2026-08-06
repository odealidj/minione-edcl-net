using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Jobs;

public interface IGpsSyncJob
{
    Task ExecuteAsync(long logisticPartnerId, long gpsVendorId, CancellationToken cancellationToken);
}
