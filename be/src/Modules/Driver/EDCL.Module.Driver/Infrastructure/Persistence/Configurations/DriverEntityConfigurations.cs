using EDCL.Module.Driver.Domain.Entities;
using EDCL.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EDCL.Module.Driver.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();

        builder.Property(s => s.SupplierCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.Latitude).HasColumnType("float");
        builder.Property(s => s.Longitude).HasColumnType("float");
        builder.Property(s => s.GeofenceRadiusMeters).HasColumnType("int");
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        ConfigureAuditColumns(builder);

        builder.HasIndex(s => s.SupplierCode).IsUnique().HasDatabaseName("UQ_suppliers_code");
        builder.HasIndex(s => s.IsActive).HasDatabaseName("IX_suppliers_is_active");
    }

    private static void ConfigureAuditColumns<T>(EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnName("created_at")
            .HasColumnType("datetime2(7)").HasDefaultValueSql("GETUTCDATE()").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2(7)");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("datetime2(7)");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(100);
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.Property(e => e.TraceId).HasColumnName("trace_id").HasMaxLength(64);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

internal sealed class TruckConfiguration : IEntityTypeConfiguration<Truck>
{
    public void Configure(EntityTypeBuilder<Truck> builder)
    {
        builder.ToTable("trucks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();

        builder.Property(t => t.PlateNumber).HasMaxLength(20).IsRequired();
        builder.Property(t => t.VehicleType).HasMaxLength(100);
        builder.Property(t => t.IsActive).HasDefaultValue(true);

        ConfigureAuditColumns(builder);

        builder.HasIndex(t => t.PlateNumber).IsUnique().HasDatabaseName("UQ_trucks_plate_number");
    }

    private static void ConfigureAuditColumns<T>(EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnName("created_at")
            .HasColumnType("datetime2(7)").HasDefaultValueSql("GETUTCDATE()").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2(7)");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("datetime2(7)");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by").HasMaxLength(100);
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.Property(e => e.TraceId).HasColumnName("trace_id").HasMaxLength(64);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
