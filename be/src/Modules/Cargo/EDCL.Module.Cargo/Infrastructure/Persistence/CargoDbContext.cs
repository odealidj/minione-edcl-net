namespace EDCL.Module.Cargo.Infrastructure.Persistence;

using EDCL.Module.Cargo.Domain.Entities;
using EDCL.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class CargoDbContext(DbContextOptions<CargoDbContext> options) : DbContext(options)
{
    public DbSet<Manifest> Manifests => Set<Manifest>();
    public DbSet<ManifestPart> ManifestParts => Set<ManifestPart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ingestion");

        modelBuilder.Entity<Manifest>(b =>
        {
            b.ToTable("manifests");
            b.HasKey(x => x.Id);
            b.Property(x => x.ManifestNo).HasMaxLength(50).IsRequired();
            b.Property(x => x.SupplierCode).HasMaxLength(20).IsRequired();
            b.Property(x => x.SupplierName).HasMaxLength(100).IsRequired();
            b.Property(x => x.OrderType).HasMaxLength(10).IsRequired();
            b.Property(x => x.Cycle).HasMaxLength(10).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();

            b.HasMany(x => x.Parts)
             .WithOne(p => p.Manifest)
             .HasForeignKey(p => p.ManifestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ManifestPart>(b =>
        {
            b.ToTable("manifest_parts");
            b.HasKey(x => x.Id);
            b.Property(x => x.PartNo).HasMaxLength(50).IsRequired();
            b.Property(x => x.PartName).HasMaxLength(100).IsRequired();
            b.Property(x => x.KanbanNo).HasMaxLength(50).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
