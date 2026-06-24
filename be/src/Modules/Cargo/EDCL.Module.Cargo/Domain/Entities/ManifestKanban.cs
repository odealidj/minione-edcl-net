namespace EDCL.Module.Cargo.Domain.Entities;

using EDCL.Shared.Kernel.Domain;

/// <summary>
/// Represents the kanban master data associated with a manifest.
/// Schema: [ingestion].[manifest_kanbans]
/// </summary>
public sealed class ManifestKanban : AuditableEntity
{
    public long Id { get; private set; }
    public long ManifestId { get; private set; }
    public string PartNo { get; private set; } = default!;
    public string KanbanCd { get; private set; } = default!;

    public Manifest? Manifest { get; private set; }

    private ManifestKanban() { } // EF Core

    public ManifestKanban(long manifestId, string partNo, string kanbanCd)
    {
        ManifestId = manifestId;
        PartNo = partNo;
        KanbanCd = kanbanCd;
    }
}
