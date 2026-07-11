using EDCL.Shared.Kernel.Domain;

namespace EDCL.Module.Driver.Domain.Entities;

public sealed class Transporter : AuditableEntity
{
    public long Id { get; private set; }
    public string Name { get; private set; } = default!;
    
    // Using simple creation method
    private Transporter() { }

    public static Transporter Create(string name)
    {
        return new Transporter
        {
            Name = name
        };
    }

    public void Update(string name)
    {
        Name = name;
    }
}
