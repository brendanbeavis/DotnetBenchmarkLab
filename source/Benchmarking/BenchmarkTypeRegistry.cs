using DotNetPerformanceLab.Benchmarks.Collections;
using DotNetPerformanceLab.Benchmarks.Database;
using DotNetPerformanceLab.Benchmarks.Json;

namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkTypeRegistry
{
    public static IReadOnlyList<Type> GetAllBenchmarkTypes() =>
    [
        typeof(DatabaseReportingBenchmarks),
        typeof(DatabaseReportingProjectionBenchmarks),
        typeof(CollectionAggregationBenchmarks),
        typeof(JsonPayloadParsingBenchmarks),
        typeof(JsonPayloadEndToEndBenchmarks)
    ];

    public static IReadOnlyList<Type> GetBenchmarkTypes(BenchmarkScenarioId scenarioId, JsonBenchmarkGroup jsonGroup = JsonBenchmarkGroup.All) => scenarioId switch
    {
        BenchmarkScenarioId.Database => [typeof(DatabaseReportingBenchmarks), typeof(DatabaseReportingProjectionBenchmarks)],
        BenchmarkScenarioId.Collections => [typeof(CollectionAggregationBenchmarks)],
        BenchmarkScenarioId.Json => GetJsonBenchmarkTypes(jsonGroup),
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unsupported scenario.")
    };

    private static IReadOnlyList<Type> GetJsonBenchmarkTypes(JsonBenchmarkGroup jsonGroup) => jsonGroup switch
    {
        JsonBenchmarkGroup.All => [typeof(JsonPayloadParsingBenchmarks), typeof(JsonPayloadEndToEndBenchmarks)],
        JsonBenchmarkGroup.ParserOnly => [typeof(JsonPayloadParsingBenchmarks)],
        JsonBenchmarkGroup.EndToEnd => [typeof(JsonPayloadEndToEndBenchmarks)],
        _ => throw new ArgumentOutOfRangeException(nameof(jsonGroup), jsonGroup, "Unsupported JSON benchmark group.")
    };
}
