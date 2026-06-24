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
    public DateTime PickDate { get; private set; }
    public string Cycle { get; private set; } = default!;
    
    // Status can be Pending, Scanned, etc.
    public string Status { get; private set; } = "Pending";

    // Navigations
    private readonly List<ManifestPart> _parts = new();
    public IReadOnlyCollection<ManifestPart> Parts => _parts.AsReadOnly();

    protected Manifest() { } // EF Core

    public Manifest(string manifestNo, string supplierCode, string supplierName, int sequence, DateTime pickDate, string cycle)
    {
        ManifestNo = manifestNo;
        SupplierCode = supplierCode;
        SupplierName = supplierName;
        Sequence = sequence;
        PickDate = pickDate;
        Cycle = cycle;
    }
}
