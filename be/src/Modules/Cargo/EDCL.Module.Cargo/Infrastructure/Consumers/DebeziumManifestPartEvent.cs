using System.Text.Json.Serialization;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class DebeziumManifestPartEvent
{
    [JsonPropertyName("payload")]
    public DebeziumManifestPartPayload? Payload { get; set; }
}

public class DebeziumManifestPartPayload
{
    [JsonPropertyName("before")]
    public ManifestPartDto? Before { get; set; }

    [JsonPropertyName("after")]
    public ManifestPartDto? After { get; set; }

    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;
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
