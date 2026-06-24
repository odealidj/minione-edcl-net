namespace EDCL.Shared.Kernel.Domain;

/// <summary>
/// Base class for all domain entities.
/// Carries audit trail populated automatically by EF Core SaveChanges interceptor
/// via CurrentUserService (JWT claims), NOT by manual setter.
/// </summary>
public abstract class AuditableEntity
{
    // ── Primary Key (convention: overridden in derived entity) ───────────
    // Subclass MUST declare its own [Id] property.

    // ── Audit Columns (populated by AuditSaveChangesInterceptor) ────────
    public DateTime CreatedAt { get; set; }

    /// <summary>Populated from JWT sub/driver_id claim by interceptor.</summary>
    public string CreatedBy { get; set; } = "SYSTEM";

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Populated from JWT sub/driver_id claim by interceptor.</summary>
    public string? UpdatedBy { get; set; }

    // ── Soft Delete ──────────────────────────────────────────────────────
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>Populated from JWT sub/driver_id claim by interceptor.</summary>
    public string? DeletedBy { get; private set; }

    // ── Concurrency ──────────────────────────────────────────────────────
    /// <summary>SQL Server ROWVERSION — handled by EF Core as concurrency token.</summary>
    public byte[] RowVersion { get; set; } = [];

    // ── Observability ────────────────────────────────────────────────────
    /// <summary>Trace ID from the originating HTTP request (X-Trace-Id header).</summary>
    public string? TraceId { get; set; }

    // ── Soft Delete Methods ──────────────────────────────────────────────
    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
