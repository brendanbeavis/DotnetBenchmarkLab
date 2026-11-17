using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotNetPerformanceLab.Models.Json;

public sealed class ApiOrderDto
{
    [JsonPropertyName("orderId")]
    [JsonProperty("orderId")]
    public int OrderId { get; set; }

    [JsonPropertyName("status")]
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("orderDateUtc")]
    [JsonProperty("orderDateUtc")]
    public DateTime OrderDateUtc { get; set; }

    [JsonPropertyName("customer")]
    [JsonProperty("customer")]
    public ApiCustomerDto Customer { get; set; } = new();

    [JsonPropertyName("lineItems")]
    [JsonProperty("lineItems")]
    public List<ApiOrderLineItemDto> LineItems { get; set; } = [];
}
