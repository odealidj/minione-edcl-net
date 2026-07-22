using Microsoft.EntityFrameworkCore;
using EDCL.Module.Cargo.Domain.Entities;

namespace EDCL.Module.Cargo.Infrastructure.Persistence;

public class IdcsDbContext : DbContext
{
    public IdcsDbContext(DbContextOptions<IdcsDbContext> options) : base(options)
    {
    }

    public DbSet<IdcsDeliveryStatus> IdcsDeliveryStatuses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<IdcsDeliveryStatus>(entity =>
        {
            entity.ToTable("edcl_delivery_status");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ManifestNo)
                .HasMaxLength(50);
                
            entity.Property(e => e.Status)
                .HasMaxLength(50);
        });
    }
}
