using EDCL.Module.Driver.Domain.Entities;
using EDCL.Module.Driver.Infrastructure.Persistence;
using EDCL.Module.Job.Domain.Entities;
using EDCL.Module.Job.Infrastructure.Persistence;
using EDCL.Shared.Kernel.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EDCL.Worker.GpsTracker.Consumers;

public class JobStartedConsumer(IServiceProvider serviceProvider, IPublishEndpoint publishEndpoint, ILogger<JobStartedConsumer> logger) : IConsumer<JobStartedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<JobStartedIntegrationEvent> context)
    {
        var evt = context.Message;
        
        using var scope = serviceProvider.CreateScope();
        var driverDbContext = scope.ServiceProvider.GetRequiredService<DriverDbContext>();
        var jobDbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        var truck = await driverDbContext.Set<Truck>().FirstOrDefaultAsync(t => t.Id == evt.TruckId);
        if (truck == null || !truck.IsSimulated)
        {
            return;
        }

        // Get job destination (supplier)
        var job = await jobDbContext.Set<PickupOrder>()
            .Include(j => j.Details)
            .FirstOrDefaultAsync(j => j.Id == evt.JobId);

        if (job == null || !job.Details.Any()) return;

        var firstStop = job.Details.OrderBy(d => d.Sequence).First();
        var supplier = await driverDbContext.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == firstStop.SupplierId);

        if (supplier == null || supplier.Latitude == null || supplier.Longitude == null)
        {
            logger.LogWarning("Supplier for Job {JobId} not found or missing coordinates", evt.JobId);
            return;
        }

        // Dummy Start coordinates (TMMIN Plant for example)
        double startLat = -6.317423;
        double startLon = 107.143744;

        var simulationEvt = new SimulationStartedIntegrationEvent
        {
            TruckId = truck.Id,
            GpsVehicleId = truck.GpsVehicleId ?? $"SIM-{truck.Id}",
            JobId = job.Id,
            StartLat = startLat,
            StartLon = startLon,
            EndLat = supplier.Latitude.Value,
            EndLon = supplier.Longitude.Value
        };

        await publishEndpoint.Publish(simulationEvt);
        logger.LogInformation("Triggered simulation for Job {JobId} Truck {TruckId}", job.Id, truck.Id);
    }
}
