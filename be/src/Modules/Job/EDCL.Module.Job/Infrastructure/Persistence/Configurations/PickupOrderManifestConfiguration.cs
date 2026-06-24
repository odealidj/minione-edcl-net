using EDCL.Module.Job.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EDCL.Module.Job.Infrastructure.Persistence.Configurations;

public class PickupOrderManifestConfiguration : IEntityTypeConfiguration<PickupOrderManifest>
{
    public void Configure(EntityTypeBuilder<PickupOrderManifest> builder)
    {
        builder.ToTable("pickup_order_manifests");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.ManifestNo)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.ManifestNo).IsUnique();

        builder.HasMany(x => x.Kanbans)
            .WithOne(x => x.Manifest)
            .HasForeignKey(x => x.PickupOrderManifestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit columns
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(50);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedBy).HasMaxLength(50);
        builder.Property(x => x.DeletedAt);
        builder.Property(x => x.DeletedBy).HasMaxLength(50);
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
