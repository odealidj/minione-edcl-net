namespace EDCL.Module.Cargo.Infrastructure.Persistence;

using EDCL.Module.Cargo.Domain.Entities;
using EDCL.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class CargoDbContext(DbContextOptions<CargoDbContext> options) : DbContext(options)
{
    public DbSet<Manifest> Manifests => Set<Manifest>();
    public DbSet<ManifestPart> ManifestParts => Set<ManifestPart>();
    public DbSet<ManifestKanban> ManifestKanbans => Set<ManifestKanban>();
    public DbSet<ManifestSkid> ManifestSkids => Set<ManifestSkid>();
    public DbSet<IngestionError> IngestionErrors => Set<IngestionError>();

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
            b.Property(x => x.OrderType).HasColumnName("order_type").HasMaxLength(10).IsRequired();
            b.Property(x => x.OrderNo).HasColumnName("order_no").HasMaxLength(50).IsRequired();
            b.Property(x => x.DockCode).HasColumnName("dock_cd").HasMaxLength(20).IsRequired();
            b.Property(x => x.PLaneNo).HasColumnName("p_lane_no").HasMaxLength(20).IsRequired();
            b.Property(x => x.Cycle).HasColumnName("cycle").HasMaxLength(10).IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();

            b.HasMany(x => x.Parts)
             .WithOne(p => p.Manifest)
             .HasForeignKey(p => p.ManifestId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Kanbans)
             .WithOne(k => k.Manifest)
             .HasForeignKey(k => k.ManifestId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Skids)
             .WithOne(s => s.Manifest)
             .HasForeignKey(s => s.ManifestId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ManifestPart>(b =>
        {
            b.ToTable("manifest_parts");
            b.HasKey(x => x.Id);
            b.Property(x => x.PartNo).HasMaxLength(50).IsRequired();
            b.Property(x => x.PartName).HasMaxLength(100).IsRequired();
            b.Property(x => x.KanbanNo).HasColumnName("kanban_no").HasMaxLength(50).IsRequired();
            b.Property(x => x.UniqNo).HasColumnName("uniq_no").HasMaxLength(50).IsRequired();
            b.Property(x => x.BoxType).HasColumnName("box_type").HasMaxLength(20).IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<ManifestKanban>(b =>
        {
            b.ToTable("manifest_kanbans");
            b.HasKey(x => x.Id);
            b.Property(x => x.PartNo).HasMaxLength(50).IsRequired();
            b.Property(x => x.KanbanCd).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<ManifestSkid>(b =>
        {
            b.ToTable("manifest_skids");
            b.HasKey(x => x.Id);
            b.Property(x => x.SkidNo).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<IngestionError>(b =>
        {
            b.ToTable("ingestion_errors");
            b.HasKey(x => x.Id);
            b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.StackTrace).HasColumnType("nvarchar(max)");
        });

        base.OnModelCreating(modelBuilder);
    }
}
