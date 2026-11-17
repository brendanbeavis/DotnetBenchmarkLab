using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotNetPerformanceLab.Models.Json;

public sealed class ApiOrdersPayloadDto
{
    [JsonPropertyName("metadata")]
    [JsonProperty("metadata")]
    public ApiPayloadMetadataDto Metadata { get; set; } = new();

    [JsonPropertyName("orders")]
    [JsonProperty("orders")]
    public List<ApiOrderDto> Orders { get; set; } = [];
}
