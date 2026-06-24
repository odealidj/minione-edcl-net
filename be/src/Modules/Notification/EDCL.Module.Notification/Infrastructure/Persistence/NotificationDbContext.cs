namespace EDCL.Module.Notification.Infrastructure.Persistence;

using EDCL.Module.Notification.Domain.Entities;
using EDCL.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class NotificationDbContext(
    DbContextOptions<NotificationDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor) 
    : DbContext(options)
{
    public DbSet<DriverNotification> DriverNotifications => Set<DriverNotification>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(auditInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notification");

        modelBuilder.Entity<DriverNotification>(b =>
        {
            b.ToTable("driver_notifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Message).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Type).HasMaxLength(50).IsRequired();

            b.HasIndex(x => x.DriverId);
        });

        base.OnModelCreating(modelBuilder);
    }
}
