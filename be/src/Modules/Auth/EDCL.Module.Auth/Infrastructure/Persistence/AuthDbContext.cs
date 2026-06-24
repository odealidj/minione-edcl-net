using EDCL.Module.Auth.Domain.Entities;
using EDCL.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EDCL.Module.Auth.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Auth module.
/// Owns: [auth].drivers, [auth].transporters,
///       [auth].driver_phone_histories, [auth].refresh_tokens
///
/// This context NEVER queries tables from other module schemas.
/// Cross-domain reads go through port interfaces.
/// </summary>
public sealed class AuthDbContext(
    DbContextOptions<AuthDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : DbContext(options)
{
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Transporter> Transporters => Set<Transporter>();
    public DbSet<DriverPhoneHistory> DriverPhoneHistories => Set<DriverPhoneHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);

        // Default schema for all tables in this module
        modelBuilder.HasDefaultSchema("auth");
    }
}
