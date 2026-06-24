using EDCL.Module.Job.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EDCL.Module.Job.Infrastructure.Persistence.Configurations;

public class PickupOrderConfiguration : IEntityTypeConfiguration<PickupOrder>
{
    public void Configure(EntityTypeBuilder<PickupOrder> builder)
    {
        builder.ToTable("pickup_orders");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.PoNo)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.PoNo).IsUnique();

        builder.HasMany(x => x.Details)
            .WithOne(x => x.PickupOrder)
            .HasForeignKey(x => x.PickupOrderId)
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
