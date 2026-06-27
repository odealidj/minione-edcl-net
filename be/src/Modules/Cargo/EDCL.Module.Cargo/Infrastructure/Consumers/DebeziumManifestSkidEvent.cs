using System.Text.Json.Serialization;

namespace EDCL.Module.Cargo.Infrastructure.Consumers;

public class DebeziumManifestSkidEvent
{
    [JsonPropertyName("payload")]
    public DebeziumManifestSkidPayload? Payload { get; set; }
}

public class DebeziumManifestSkidPayload
{
    [JsonPropertyName("before")]
    public ManifestSkidDto? Before { get; set; }

    [JsonPropertyName("after")]
    public ManifestSkidDto? After { get; set; }

    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;
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
