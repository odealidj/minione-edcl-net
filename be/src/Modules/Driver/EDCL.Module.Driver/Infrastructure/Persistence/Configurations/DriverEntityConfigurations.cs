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

        builder.Property(t => t.GpsVehicleId).HasMaxLength(100);
        builder.Property(t => t.IsSimulated).HasDefaultValue(false);

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
        builder.Property(r => r.LogisticPartnerId).IsRequired();

        ConfigureAuditColumns(builder);

        builder.HasIndex(r => new { r.RouteCode, r.CycleCode }).IsUnique().HasDatabaseName("UQ_routes_route_cycle");

        builder.HasOne(r => r.LogisticPartner)
            .WithMany()
            .HasForeignKey(r => r.LogisticPartnerId)
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

internal sealed class RoutePriceConfiguration : IEntityTypeConfiguration<RoutePrice>
{
    public void Configure(EntityTypeBuilder<RoutePrice> builder)
    {
        builder.ToTable("route_prices");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Id).UseIdentityColumn();

        builder.Property(rp => rp.RouteId).IsRequired();
        builder.Property(rp => rp.LogisticPartnerId).IsRequired();
        builder.Property(rp => rp.Price).HasColumnType("numeric(15,2)").IsRequired();
        builder.Property(rp => rp.ValidFrom).HasColumnType("date").IsRequired();
        builder.Property(rp => rp.ValidTo).HasColumnType("date").IsRequired();
        builder.Property(rp => rp.PriceType).HasMaxLength(1).IsRequired();

        ConfigureAuditColumns(builder);

        builder.HasIndex(rp => new { rp.RouteId, rp.LogisticPartnerId, rp.ValidFrom, rp.ValidTo, rp.PriceType })
            .IsUnique()
            .HasDatabaseName("UQ_route_prices_route_lp_dates_type");

        builder.HasOne(rp => rp.Route)
            .WithMany()
            .HasForeignKey(rp => rp.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rp => rp.LogisticPartner)
            .WithMany()
            .HasForeignKey(rp => rp.LogisticPartnerId)
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

internal sealed class TruckLocationConfiguration : IEntityTypeConfiguration<TruckLocation>
{
    public void Configure(EntityTypeBuilder<TruckLocation> builder)
    {
        builder.ToTable("truck_locations");
        builder.HasKey(tl => tl.Id);
        builder.Property(tl => tl.Id).UseIdentityColumn();

        builder.Property(tl => tl.TruckId).IsRequired();
        builder.Property(tl => tl.Latitude).HasColumnType("float").IsRequired();
        builder.Property(tl => tl.Longitude).HasColumnType("float").IsRequired();
        builder.Property(tl => tl.Speed).HasColumnType("float");
        builder.Property(tl => tl.Heading).HasColumnType("float");
        builder.Property(tl => tl.Timestamp).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(tl => tl.ProviderName).HasMaxLength(100);

        ConfigureAuditColumns(builder);

        builder.HasIndex(tl => tl.TruckId).HasDatabaseName("IX_truck_locations_truck_id");
        builder.HasIndex(tl => tl.Timestamp).HasDatabaseName("IX_truck_locations_timestamp");

        builder.HasOne(tl => tl.Truck)
            .WithMany()
            .HasForeignKey(tl => tl.TruckId)
            .OnDelete(DeleteBehavior.Cascade);
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

internal sealed class GpsVendorConfiguration : IEntityTypeConfiguration<GpsVendor>
{
    public void Configure(EntityTypeBuilder<GpsVendor> builder)
    {
        builder.ToTable("gps_vendors");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();
        
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.ProviderType).HasConversion<int>().IsRequired();
        
        builder.Property(t => t.ApiUrl).HasMaxLength(500);
        builder.Property(t => t.ApiUsername).HasMaxLength(200);
        builder.Property(t => t.ApiPassword).HasMaxLength(200);
        builder.Property(t => t.ApiToken).HasMaxLength(1000);

        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("UQ_gps_vendors_code");

        ConfigureAuditColumns(builder);
    }

    private static void ConfigureAuditColumns<T>(EntityTypeBuilder<T> builder) where T : AuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(7)").HasDefaultValueSql("GETUTCDATE()").IsRequired();
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

internal sealed class LogisticPartnerGpsVendorConfiguration : IEntityTypeConfiguration<LogisticPartnerGpsVendor>
{
    public void Configure(EntityTypeBuilder<LogisticPartnerGpsVendor> builder)
    {
        builder.ToTable("logistic_partner_gps_vendors");
        
        builder.HasKey(t => new { t.LogisticPartnerId, t.GpsVendorId });

        builder.Property(t => t.LastGpsSyncAt).HasColumnType("datetime2(7)");
        builder.Property(t => t.LastGpsSyncStatus).HasMaxLength(50);
        builder.Property(t => t.LastGpsSyncMessage).HasMaxLength(2000);

        builder.HasOne(t => t.LogisticPartner)
            .WithMany(lp => lp.GpsVendorMappings)
            .HasForeignKey(t => t.LogisticPartnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.GpsVendor)
            .WithMany(gv => gv.LogisticPartnerMappings)
            .HasForeignKey(t => t.GpsVendorId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureAuditColumns(builder);
    }

    private static void ConfigureAuditColumns<T>(EntityTypeBuilder<T> builder) where T : AuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(7)").HasDefaultValueSql("GETUTCDATE()").IsRequired();
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
