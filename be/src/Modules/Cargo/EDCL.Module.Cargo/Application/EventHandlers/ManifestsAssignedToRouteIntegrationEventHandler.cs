using EDCL.Module.Cargo.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EDCL.Module.Cargo.Application.EventHandlers;

internal sealed class ManifestsAssignedToRouteIntegrationEventHandler(CargoDbContext dbContext) 
    : INotificationHandler<ManifestsAssignedToRouteIntegrationEvent>
{
    public async Task Handle(ManifestsAssignedToRouteIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (notification.ManifestNos == null || !notification.ManifestNos.Any())
            return;

        var manifests = await dbContext.Manifests
            .Where(x => notification.ManifestNos.Contains(x.ManifestNo))
            .ToListAsync(cancellationToken);

        foreach (var manifest in manifests)
        {
            if (notification.IsAssigned)
            {
                manifest.AssignToRoute();
            }
            else
            {
                manifest.UnassignFromRoute();
            }
        }

        if (manifests.Any())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
