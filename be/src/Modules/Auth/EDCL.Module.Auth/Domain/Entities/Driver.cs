using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Auth.Domain.Entities;

/// <summary>
/// Driver entity — primary identity for the mobile app user.
/// Schema: [auth].[drivers]
/// </summary>
public sealed class Driver : AuditableEntity
{
    public long Id { get; private set; }
    public long? LogisticPartnerId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Nik { get; private set; } = default!;           // Immutable — national ID
    public string PhoneNumber { get; private set; } = default!;
    public string PinHash { get; private set; } = default!;
    public bool MustChangePin { get; private set; } = true;
    public string? FcmToken { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? PhotoUrl { get; private set; }

    // Navigation

    public ICollection<DriverPhoneHistory> PhoneHistories { get; private set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    private Driver() { } // EF Core

    public static Driver Create(
        string name, string nik, string phoneNumber,
        string pinHash, long? logisticPartnerId = null)
        => new()
        {
            Name = name,
            Nik = nik,
            PhoneNumber = phoneNumber,
            PinHash = pinHash,
            MustChangePin = true,
            LogisticPartnerId = logisticPartnerId,
            IsActive = true
        };

    public void UpdateFcmToken(string? fcmToken) => FcmToken = fcmToken;

    public void ChangePin(string newPinHash)
    {
        PinHash = newPinHash;
        MustChangePin = false;
    }

    public void ChangePhoneNumber(string newPhone)
    {
        // Trigger will auto-log to driver_phone_histories in DB
        PhoneNumber = newPhone;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
