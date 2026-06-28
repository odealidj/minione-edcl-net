namespace EDCL.Module.Cargo.Domain.Entities;

using EDCL.Shared.Kernel.Domain;

public class ManifestPart : AuditableEntity
{
    public long Id { get; private set; }
    public long ManifestId { get; private set; }
    public string PartNo { get; private set; } = default!;
    public string PartName { get; private set; } = default!;
    public int Qty { get; private set; }
    public string KanbanNo { get; private set; } = default!;
    public string UniqNo { get; private set; } = default!;
    public string BoxType { get; private set; } = default!;
    public string Status { get; private set; } = "Pending";

    public Manifest Manifest { get; private set; } = default!;

    protected ManifestPart() { } // EF Core

    public ManifestPart(long manifestId, string partNo, string partName, int qty, string kanbanNo, string uniqNo, string boxType)
    {
        ManifestId = manifestId;
        PartNo = partNo;
        PartName = partName;
        Qty = qty;
        KanbanNo = kanbanNo;
        UniqNo = uniqNo;
        BoxType = boxType;
    }
}
