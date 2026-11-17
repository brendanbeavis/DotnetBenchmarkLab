using System.Text.Json.Serialization;
using DotNetPerformanceLab.Models.Json;

namespace DotNetPerformanceLab.Data.Json;

[JsonSerializable(typeof(ApiOrdersPayloadDto))]
[JsonSerializable(typeof(ApiOrderDto))]
[JsonSerializable(typeof(ApiOrderLineItemDto))]
[JsonSerializable(typeof(ApiCustomerDto))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
public partial class JsonSerializationContexts : JsonSerializerContext
{
}
