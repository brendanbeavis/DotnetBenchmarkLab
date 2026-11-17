using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotNetPerformanceLab.Models.Json;

public sealed class ApiCustomerDto
{
    [JsonPropertyName("customerId")]
    [JsonProperty("customerId")]
    public int CustomerId { get; set; }

    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    [JsonProperty("region")]
    public string Region { get; set; } = string.Empty;
}
