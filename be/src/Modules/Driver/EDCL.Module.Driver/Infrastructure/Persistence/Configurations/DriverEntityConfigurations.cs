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

        builder.Property(t => t.LogisticPartnerId).IsRequired();

        ConfigureAuditColumns(builder);

        builder.HasIndex(t => t.PlateNumber).IsUnique().HasDatabaseName("UQ_trucks_plate_number");

        builder.HasOne(t => t.LogisticPartner)
            .WithMany() // LogisticPartner doesn't have a navigation collection for Trucks right now
            .HasForeignKey(t => t.LogisticPartnerId)
            .OnDelete(DeleteBehavior.Restrict);
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

internal sealed class LogisticPartnerConfiguration : IEntityTypeConfiguration<LogisticPartner>
{
    public void Configure(EntityTypeBuilder<LogisticPartner> builder)
    {
        builder.ToTable("logistic_partners");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();
        builder.Property(t => t.Code).HasMaxLength(10).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();

        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("UQ_logistic_partners_code");

        ConfigureAuditColumns(builder);
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

internal sealed class TruckDriverAssignmentConfiguration : IEntityTypeConfiguration<TruckDriverAssignment>
{
    public void Configure(EntityTypeBuilder<TruckDriverAssignment> builder)
    {
        builder.ToTable("truck_driver_assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityColumn();

        builder.Property(a => a.TruckId).IsRequired();
        builder.Property(a => a.DriverId).IsRequired();
        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder.Property(a => a.AssignedAt).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(a => a.UnassignedAt).HasColumnType("datetime2(7)");

        ConfigureAuditColumns(builder);

        builder.HasIndex(a => new { a.TruckId, a.IsActive }).HasDatabaseName("IX_assignments_truck_active");
        builder.HasIndex(a => new { a.DriverId, a.IsActive }).HasDatabaseName("IX_assignments_driver_active");

        builder.HasOne(a => a.Truck)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TruckId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // No navigation property from Driver since Driver is in Auth module.
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

internal sealed class RouteConfiguration : IEntityTypeConfiguration<EDCL.Module.Driver.Domain.Entities.Route>
{
    public void Configure(EntityTypeBuilder<EDCL.Module.Driver.Domain.Entities.Route> builder)
    {
        builder.ToTable("routes");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();

        builder.Property(r => r.RouteCode).HasMaxLength(50).IsRequired();
        builder.Property(r => r.CycleCode).HasMaxLength(10).IsRequired();

        ConfigureAuditColumns(builder);

        builder.HasIndex(r => new { r.RouteCode, r.CycleCode }).IsUnique().HasDatabaseName("UQ_routes_route_cycle");
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
