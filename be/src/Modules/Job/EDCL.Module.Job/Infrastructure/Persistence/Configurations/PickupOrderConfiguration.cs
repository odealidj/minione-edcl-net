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
            .HasMaxLength(50)
            .HasColumnName("delivery_no");
            
        builder.Property(x => x.PickupDate)
            .IsRequired()
            .HasColumnType("date")
            .HasColumnName("pickup_date");

        builder.Property(x => x.RouteCode)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("route_code");

        builder.Property(x => x.CycleCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasColumnName("cycle_code");

        builder.Property(x => x.EstimatedDepartureTime)
            .IsRequired()
            .HasColumnType("time(0)")
            .HasColumnName("estimated_departure_time");
            
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

        builder.Property(x => x.HangfireJobIdH1).HasMaxLength(100);
        builder.Property(x => x.HangfireJobIdH30).HasMaxLength(100);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
