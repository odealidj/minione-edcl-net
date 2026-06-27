using System.Text.Json.Serialization;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

/// <summary>
/// DTO representing the standard Debezium CDC payload structure.
/// </summary>
public class DebeziumManifestEvent
{
    [JsonPropertyName("payload")]
    public DebeziumPayload? Payload { get; set; }
}

public class DebeziumPayload
{
    [JsonPropertyName("before")]
    public ManifestDto? Before { get; set; }

    [JsonPropertyName("after")]
    public ManifestDto? After { get; set; }

    /// <summary>
    /// Operation: "c" for create, "u" for update, "d" for delete, "r" for read (snapshot).
    /// </summary>
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;
}

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
