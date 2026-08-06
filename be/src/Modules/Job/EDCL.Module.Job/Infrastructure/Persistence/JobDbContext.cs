using EDCL.Module.Job.Domain.Entities;
using EDCL.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Job.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Job module.
/// Schema: [job]
/// </summary>
public sealed class JobDbContext(
    DbContextOptions<JobDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : DbContext(options)
{
    public DbSet<PickupOrder> PickupOrders => Set<PickupOrder>();
    public DbSet<PickupOrderDetail> PickupOrderDetails => Set<PickupOrderDetail>();
    public DbSet<PickupOrderManifest> PickupOrderManifests => Set<PickupOrderManifest>();
    public DbSet<PickupOrderKanban> PickupOrderKanbans => Set<PickupOrderKanban>();
    public DbSet<LiveTrackingFleet> LiveTrackingFleets => Set<LiveTrackingFleet>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(auditInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JobDbContext).Assembly);
        modelBuilder.HasDefaultSchema("job");
    }
}
