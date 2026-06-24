namespace EDCL.Shared.Infrastructure.Persistence;

/// <summary>
/// Provides the current authenticated user's identity from JWT claims.
/// Injected by DI — used by AuditSaveChangesInterceptor to populate
/// created_by / updated_by / deleted_by columns automatically.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Driver ID from JWT 'sub' or 'driver_id' claim. Null if unauthenticated.</summary>
    long? DriverId { get; }

    /// <summary>Human-readable identity string for audit columns.</summary>
    string AuditIdentity { get; }

    /// <summary>Trace ID from HTTP context for correlation.</summary>
    string? CurrentTraceId { get; }

    bool IsAuthenticated { get; }
}

/// <summary>
/// Provides thread-safe access to the HTTP context claim values.
/// Implemented in the API host layer to avoid dependency on Microsoft.AspNetCore.*
/// from the Infrastructure library.
///
/// The Func delegates are resolved each request (scoped lifetime).
/// </summary>
public sealed class CurrentUserService(
    Func<long?> getDriverId,
    Func<string?> getTraceId,
    Func<bool> getIsAuthenticated)
    : ICurrentUserService
{
    public long? DriverId => getDriverId();
    public bool IsAuthenticated => getIsAuthenticated();

    public string AuditIdentity => DriverId.HasValue
        ? $"DRIVER:{DriverId}"
        : "SYSTEM";

    public string? CurrentTraceId => getTraceId();
}

/// <summary>Lightweight identity for background workers (no HTTP context).</summary>
public sealed class SystemUserService : ICurrentUserService
{
    public long? DriverId        => null;
    public string AuditIdentity  => "SYSTEM";
    public string? CurrentTraceId => null;
    public bool IsAuthenticated  => false;
}
