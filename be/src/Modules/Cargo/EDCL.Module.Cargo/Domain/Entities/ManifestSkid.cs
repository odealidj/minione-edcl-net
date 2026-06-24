namespace EDCL.Module.Cargo.Domain.Entities;

using EDCL.Shared.Kernel.Domain;

/// <summary>
/// Represents the skid master data associated with a manifest.
/// Schema: [ingestion].[manifest_skids]
/// </summary>
public sealed class ManifestSkid : AuditableEntity
{
    public long Id { get; private set; }
    public long ManifestId { get; private set; }
    public string SkidNo { get; private set; } = default!;

    public Manifest? Manifest { get; private set; }

    private ManifestSkid() { } // EF Core

    public ManifestSkid(long manifestId, string skidNo)
    {
        ManifestId = manifestId;
        SkidNo = skidNo;
    }
}
