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
        builder.Property(d => d.MustChangePin).HasColumnName("must_change_pin").HasDefaultValue(true);
        builder.Property(d => d.IsActive).HasDefaultValue(true);

        // ── Audit Columns ─────────────────────────────────────────────────
        ConfigureAuditColumns(builder);

        // ── Indexes ───────────────────────────────────────────────────────
        builder.HasIndex(d => d.Nik).IsUnique().HasDatabaseName("UQ_drivers_nik");

        // Partial unique index: only 1 active phone per driver
        builder.HasIndex(d => d.PhoneNumber)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
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

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();
        
        builder.Property(r => r.Code).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.IsActive).HasDefaultValue(true);

        ConfigureAuditColumns(builder);

        builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName("UQ_roles_code");

        // Seed Default Admin Role
        builder.HasData(
            new
            {
                Id = 1L,
                Code = "ADMIN",
                Name = "System Administrator",
                Description = "Has full access to all features",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "SYSTEM",
                IsDeleted = false
            },
            new
            {
                Id = 2L,
                Code = "USER",
                Name = "Standard User",
                Description = "Standard access for web client",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "SYSTEM",
                IsDeleted = false
            }
        );
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

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).UseIdentityColumn();

        builder.Property(u => u.Name).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        ConfigureAuditColumns(builder);

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_app_users_email");

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
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
