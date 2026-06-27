using System;
using System.Text.Json.Serialization;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class ManifestDto
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("ManifestNo")]
    public string ManifestNo { get; set; } = string.Empty;

    [JsonPropertyName("SupplierCode")]
    public string SupplierCode { get; set; } = string.Empty;

    [JsonPropertyName("SupplierName")]
    public string SupplierName { get; set; } = string.Empty;

    [JsonPropertyName("Sequence")]
    public int Sequence { get; set; }

    [JsonPropertyName("OrderType")]
    public string OrderType { get; set; } = "ORG";

    [JsonPropertyName("PickDate")]
    public DateTime PickDate { get; set; }

    [JsonPropertyName("Cycle")]
    public string Cycle { get; set; } = string.Empty;
    
    [JsonPropertyName("Status")]
    public string Status { get; set; } = "Pending";
}

public class ManifestPartDto
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("ManifestId")]
    public long ManifestId { get; set; }

    [JsonPropertyName("PartNo")]
    public string PartNo { get; set; } = string.Empty;

    [JsonPropertyName("PartName")]
    public string PartName { get; set; } = string.Empty;

    [JsonPropertyName("KanbanNo")]
    public string KanbanNo { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;
}

public class ManifestKanbanDto
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("ManifestId")]
    public long ManifestId { get; set; }

    [JsonPropertyName("PartNo")]
    public string PartNo { get; set; } = string.Empty;

    [JsonPropertyName("KanbanCd")]
    public string KanbanCd { get; set; } = string.Empty;
}

public class ManifestSkidDto
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("ManifestId")]
    public long ManifestId { get; set; }

    [JsonPropertyName("SkidNo")]
    public string SkidNo { get; set; } = string.Empty;
}
