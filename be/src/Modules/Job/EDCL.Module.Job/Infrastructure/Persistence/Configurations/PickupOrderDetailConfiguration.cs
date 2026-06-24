using EDCL.Module.Job.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EDCL.Module.Job.Infrastructure.Persistence.Configurations;

public class PickupOrderDetailConfiguration : IEntityTypeConfiguration<PickupOrderDetail>
{
    public void Configure(EntityTypeBuilder<PickupOrderDetail> builder)
    {
        builder.ToTable("pickup_order_details");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasMany(x => x.Manifests)
            .WithOne(x => x.Detail)
            .HasForeignKey(x => x.PickupOrderDetailId)
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
