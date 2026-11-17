using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotNetPerformanceLab.Models.Json;

public sealed class ApiPayloadMetadataDto
{
    [JsonPropertyName("requestId")]
    [JsonProperty("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("generatedAtUtc")]
    [JsonProperty("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; }

    [JsonPropertyName("sourceSystem")]
    [JsonProperty("sourceSystem")]
    public string SourceSystem { get; set; } = string.Empty;
}
