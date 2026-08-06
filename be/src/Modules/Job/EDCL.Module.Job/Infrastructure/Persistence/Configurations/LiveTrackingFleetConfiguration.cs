using EDCL.Module.Job.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EDCL.Module.Job.Infrastructure.Persistence.Configurations;

public sealed class LiveTrackingFleetConfiguration : IEntityTypeConfiguration<LiveTrackingFleet>
{
    public void Configure(EntityTypeBuilder<LiveTrackingFleet> builder)
    {
        builder.ToTable("live_tracking_fleets");

        builder.HasKey(x => x.Id);

        // Core fields
        builder.Property(x => x.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.RawData)
            .HasColumnType("nvarchar(max)"); // We might store JSON here

        // Standard Audit Columns
        builder.Property(x => x.CreatedBy).HasMaxLength(100);
        builder.Property(x => x.UpdatedBy).HasMaxLength(100);
        builder.Property(x => x.DeletedBy).HasMaxLength(100);
        builder.Property(x => x.TraceId).HasMaxLength(100);
        builder.Property(x => x.RowVersion).IsRowVersion();
        
        builder.HasIndex(x => x.TruckId);
        builder.HasIndex(x => x.RecordedAt);
    }
}
