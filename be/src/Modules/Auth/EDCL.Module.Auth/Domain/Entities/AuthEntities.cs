using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Auth.Domain.Entities;



public sealed class DriverPhoneHistory : AuditableEntity
{
    public long Id { get; private set; }
    public long DriverId { get; private set; }
    public string OldPhoneNumber { get; private set; } = default!;
    public string NewPhoneNumber { get; private set; } = default!;
    public DateTime ChangedAt { get; private set; }
    public Driver? Driver { get; private set; }
    private DriverPhoneHistory() { }

    public static DriverPhoneHistory Record(long driverId, string oldPhone, string newPhone)
        => new()
        {
            DriverId = driverId,
            OldPhoneNumber = oldPhone,
            NewPhoneNumber = newPhone,
            ChangedAt = DateTime.UtcNow
        };
}

/// <summary>
/// Refresh token entity. Each driver can have multiple active tokens
/// (multiple devices). Tokens are rotated on each refresh.
/// Schema: [auth].[refresh_tokens]
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    public long Id { get; private set; }
    public long DriverId { get; private set; }
    public string Token { get; private set; } = default!;         // Hashed token stored in DB
    public string? DeviceInfo { get; private set; }               // User-Agent or device fingerprint
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }          // Rotation tracking
    public Driver? Driver { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(long driverId, string hashedToken, int expiryDays, string? deviceInfo = null)
        => new()
        {
            DriverId = driverId,
            Token = hashedToken,
            DeviceInfo = deviceInfo,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false
        };

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    public void Revoke(string? replacedBy = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedBy;
    }
}

/// <summary>
/// Refresh token entity for AppUser (Web Dashboard). 
/// Tokens are rotated on each refresh.
/// Schema: [auth].[app_user_refresh_tokens]
/// </summary>
public sealed class AppUserRefreshToken : AuditableEntity
{
    public long Id { get; private set; }
    public long AppUserId { get; private set; }
    public string Token { get; private set; } = default!;         // Hashed token stored in DB
    public string? DeviceInfo { get; private set; }               // User-Agent or device fingerprint
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }          // Rotation tracking
    public AppUser? AppUser { get; private set; }

    private AppUserRefreshToken() { }

    public static AppUserRefreshToken Create(long appUserId, string hashedToken, int expiryDays, string? deviceInfo = null)
        => new()
        {
            AppUserId = appUserId,
            Token = hashedToken,
            DeviceInfo = deviceInfo,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false
        };

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    public void Revoke(string? replacedBy = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedBy;
    }
}
