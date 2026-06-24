using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Auth.Domain.Entities;

/// <summary>
/// Defines a dynamic Role for Role-Based Access Control (RBAC).
/// Schema: [auth].[roles]
/// </summary>
public sealed class Role : AuditableEntity
{
    public long Id { get; private set; }
    
    /// <summary>Unique string identifier, e.g., "ADMIN", "DISPATCHER"</summary>
    public string Code { get; private set; } = default!;
    
    /// <summary>Human readable name</summary>
    public string Name { get; private set; } = default!;
    
    public string? Description { get; private set; }
    
    public bool IsActive { get; private set; } = true;

    // Navigation
    public ICollection<AppUser> Users { get; private set; } = [];

    private Role() { }

    public static Role Create(string code, string name, string? description = null)
        => new()
        {
            Code = code.ToUpperInvariant(),
            Name = name,
            Description = description,
            IsActive = true
        };

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
