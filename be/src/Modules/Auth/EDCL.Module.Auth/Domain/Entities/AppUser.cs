using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Auth.Domain.Entities;

/// <summary>
/// Represents a Back-office Web User (Admin, Dispatcher, etc.)
/// Schema: [auth].[app_users]
/// </summary>
public sealed class AppUser : AuditableEntity
{
    public long Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    
    // 1-to-1 relationship with Role for simplicity.
    public long RoleId { get; private set; }
    
    public bool IsActive { get; private set; } = true;

    // Navigation
    public Role? Role { get; private set; }

    private AppUser() { }

    public static AppUser Create(string name, string email, string passwordHash, long roleId)
        => new()
        {
            Name = name,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            RoleId = roleId,
            IsActive = true
        };

    public void UpdatePassword(string newHash) => PasswordHash = newHash;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void ChangeRole(long newRoleId) => RoleId = newRoleId;
}
