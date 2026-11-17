using System.Text.Json;
using Newtonsoft.Json;

namespace DotNetPerformanceLab.Data.Json;

public static class JsonSerializationOptionsProvider
{
    public static JsonSerializerOptions Standard { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public static JsonSerializerSettings NewtonsoftSettings { get; } = new()
    {
        NullValueHandling = NullValueHandling.Include,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        DateParseHandling = DateParseHandling.DateTime,
        FloatParseHandling = FloatParseHandling.Decimal
    };
}
