namespace EDCL.Module.Cargo.Domain.Entities;

using EDCL.Shared.Kernel.Domain;

public class Manifest : AuditableEntity
{
    public long Id { get; private set; }
    public string ManifestNo { get; private set; } = default!;
    public string SupplierCode { get; private set; } = default!;
    public string SupplierName { get; private set; } = default!;
    public int Sequence { get; private set; }
    public string OrderType { get; private set; } = "ORG"; // "ORG" only based on user preference
    public string OrderNo { get; private set; } = default!;
    public string DockCode { get; private set; } = default!;
    public string PLaneNo { get; private set; } = default!;
    public DateTime PickDate { get; private set; }
    public string Cycle { get; private set; } = default!;
    
    // Status can be Pending, Scanned, etc.
    public string Status { get; private set; } = "Pending";

    private readonly List<ManifestPart> _parts = new();
    public IReadOnlyCollection<ManifestPart> Parts => _parts.AsReadOnly();

    private readonly List<ManifestKanban> _kanbans = new();
    public IReadOnlyCollection<ManifestKanban> Kanbans => _kanbans.AsReadOnly();

    private readonly List<ManifestSkid> _skids = new();
    public IReadOnlyCollection<ManifestSkid> Skids => _skids.AsReadOnly();

    protected Manifest() { } // EF Core

    public Manifest(string manifestNo, string supplierCode, string supplierName, int sequence, DateTime pickDate, string cycle, string orderNo, string dockCode, string pLaneNo)
    {
        ManifestNo = manifestNo;
        SupplierCode = supplierCode;
        SupplierName = supplierName;
        Sequence = sequence;
        PickDate = pickDate;
        Cycle = cycle;
        OrderNo = orderNo;
        DockCode = dockCode;
        PLaneNo = pLaneNo;
    }
}
