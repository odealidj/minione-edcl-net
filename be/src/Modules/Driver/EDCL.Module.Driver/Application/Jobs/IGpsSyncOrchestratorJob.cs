using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Driver.Application.Jobs;

public interface IGpsSyncOrchestratorJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
