using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DotNetPerformanceLab.Models.Json;

public sealed class ApiOrderLineItemDto
{
    [JsonPropertyName("productId")]
    [JsonProperty("productId")]
    public int ProductId { get; set; }

    [JsonPropertyName("sku")]
    [JsonProperty("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    [JsonProperty("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    [JsonProperty("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("discountPercent")]
    [JsonProperty("discountPercent")]
    public decimal DiscountPercent { get; set; }

    [JsonPropertyName("notes")]
    [JsonProperty("notes")]
    public string? Notes { get; set; }
}
