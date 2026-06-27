using System.Text.Json.Serialization;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class DebeziumManifestKanbanEvent
{
    [JsonPropertyName("payload")]
    public DebeziumManifestKanbanPayload? Payload { get; set; }
}

public class DebeziumManifestKanbanPayload
{
    [JsonPropertyName("before")]
    public ManifestKanbanDto? Before { get; set; }

    [JsonPropertyName("after")]
    public ManifestKanbanDto? After { get; set; }

    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;
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
