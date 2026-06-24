using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EDCL.Module.Auth.Infrastructure.Persistence.Configurations;

internal sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.ToTable("drivers");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).UseIdentityColumn();

        builder.Property(d => d.Name).HasMaxLength(150).IsRequired();
        builder.Property(d => d.Nik).HasMaxLength(16).IsRequired();
        builder.Property(d => d.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(d => d.PinHash).HasMaxLength(255).IsRequired();
        builder.Property(d => d.FcmToken).HasColumnType("nvarchar(max)");
        builder.Property(d => d.PhotoUrl).HasMaxLength(500);
        builder.Property(d => d.IsActive).HasDefaultValue(true);

        // ── Audit Columns ─────────────────────────────────────────────────
        ConfigureAuditColumns(builder);

        // ── Indexes ───────────────────────────────────────────────────────
        builder.HasIndex(d => d.Nik).IsUnique().HasDatabaseName("UQ_drivers_nik");

        // Partial unique index: only 1 active phone per driver
        builder.HasIndex(d => d.PhoneNumber)
            .IsUnique()
            .HasFilter("[is_active] = 1")
            .HasDatabaseName("UIX_drivers_active_phone");

        // Performance index for dashboard queries
        builder.HasIndex(d => new { d.IsActive, d.PhoneNumber })
            .HasDatabaseName("IX_drivers_active_phone_perf");

        // ── Relationships ─────────────────────────────────────────────────
        builder.HasOne(d => d.Transporter)
            .WithMany(t => t.Drivers)
            .HasForeignKey(d => d.TransporterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(d => d.PhoneHistories)
            .WithOne(ph => ph.Driver)
            .HasForeignKey(ph => ph.DriverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.RefreshTokens)
            .WithOne(rt => rt.Driver)
            .HasForeignKey(rt => rt.DriverId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAuditColumns<T>(EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnName("created_at")
            .HasColumnType("datetime2(7)").HasDefaultValueSql("GETUTCDATE()").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by")
            .HasMaxLength(100).IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at")
            .HasColumnType("datetime2(7)");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by")
            .HasMaxLength(100);
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted")
            .HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at")
            .HasColumnType("datetime2(7)");
        builder.Property(e => e.DeletedBy).HasColumnName("deleted_by")
            .HasMaxLength(100);
        builder.Property(e => e.RowVersion).HasColumnName("row_version")
            .IsRowVersion();
        builder.Property(e => e.TraceId).HasColumnName("trace_id")
            .HasMaxLength(64);

        // Global query filter: exclude soft-deleted records automatically
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

internal sealed class TransporterConfiguration : IEntityTypeConfiguration<Transporter>
{
    public void Configure(EntityTypeBuilder<Transporter> builder)
    {
        builder.ToTable("transporters");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();

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

internal sealed class DriverPhoneHistoryConfiguration : IEntityTypeConfiguration<DriverPhoneHistory>
{
    public void Configure(EntityTypeBuilder<DriverPhoneHistory> builder)
    {
        builder.ToTable("driver_phone_histories");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).UseIdentityColumn();
        builder.Property(h => h.OldPhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(h => h.NewPhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(h => h.ChangedAt).HasColumnType("datetime2(0)")
            .HasDefaultValueSql("GETUTCDATE()");
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).UseIdentityColumn();
        builder.Property(rt => rt.Token).HasMaxLength(500).IsRequired();
        builder.Property(rt => rt.DeviceInfo).HasMaxLength(500);
        builder.Property(rt => rt.ExpiresAt).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(rt => rt.RevokedAt).HasColumnType("datetime2(7)");
        builder.Property(rt => rt.ReplacedByToken).HasMaxLength(500);

        builder.HasIndex(rt => rt.Token).HasDatabaseName("IX_refresh_tokens_token");
        builder.HasIndex(rt => new { rt.DriverId, rt.IsRevoked })
            .HasDatabaseName("IX_refresh_tokens_driver_active");
    }
}
