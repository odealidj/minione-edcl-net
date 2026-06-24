using EDCL.Shared.Kernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EDCL.Shared.Infrastructure.Persistence;

/// <summary>
/// EF Core SaveChanges interceptor that automatically populates audit columns
/// from the current user's JWT claims via ICurrentUserService.
///
/// Columns populated:
///   - created_at / created_by  → on INSERT
///   - updated_at / updated_by  → on UPDATE
///   - trace_id                 → on both INSERT and UPDATE
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUserService currentUser)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now      = DateTime.UtcNow;
        var identity = currentUser.AuditIdentity;
        var traceId  = currentUser.CurrentTraceId;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = identity;
                    entry.Entity.TraceId   = traceId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = identity;
                    entry.Entity.TraceId   = traceId;
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    break;
            }
        }
    }
}
