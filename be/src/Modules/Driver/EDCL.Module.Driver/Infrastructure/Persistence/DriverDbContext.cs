using EDCL.Module.Driver.Domain.Entities;
using EDCL.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Driver.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Driver module.
/// Owns: [driver].[suppliers], [driver].[trucks], [driver].[logisticPartners], [driver].[truck_driver_assignments]
///
/// This context NEVER queries tables from other module schemas.
/// Cross-domain reads go through port interfaces.
/// </summary>
public sealed class DriverDbContext(
    DbContextOptions<DriverDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : DbContext(options)
{
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<LogisticPartner> LogisticPartners => Set<LogisticPartner>();
    public DbSet<EDCL.Module.Driver.Domain.Entities.Route> Routes => Set<EDCL.Module.Driver.Domain.Entities.Route>();
    public DbSet<RoutePrice> RoutePrices => Set<RoutePrice>();
    public DbSet<TruckDriverAssignment> TruckDriverAssignments => Set<TruckDriverAssignment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DriverDbContext).Assembly);
        modelBuilder.HasDefaultSchema("driver");
    }
}
