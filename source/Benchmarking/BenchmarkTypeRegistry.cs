using DotNetPerformanceLab.Benchmarks.Collections;
using DotNetPerformanceLab.Benchmarks.Database;
using DotNetPerformanceLab.Benchmarks.Json;
using DotNetPerformanceLab.Benchmarks.AsyncIo;

namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkTypeRegistry
{
    public static IReadOnlyList<Type> GetAllBenchmarkTypes() =>
    [
        typeof(DatabaseReportingBenchmarks),
        typeof(DatabaseReportingProjectionBenchmarks),
        typeof(CollectionAggregationBenchmarks),
        typeof(JsonPayloadParsingBenchmarks),
        typeof(JsonPayloadEndToEndBenchmarks),
        typeof(JsonPayloadStreamingParsingBenchmarks),
        typeof(JsonPayloadStreamingEndToEndBenchmarks),
        typeof(AsyncIoBatchBenchmarks)
    ];

    public static IReadOnlyList<Type> GetBenchmarkTypes(BenchmarkScenarioId scenarioId, JsonBenchmarkGroup jsonGroup = JsonBenchmarkGroup.All) => scenarioId switch
    {
        BenchmarkScenarioId.Database => [typeof(DatabaseReportingBenchmarks), typeof(DatabaseReportingProjectionBenchmarks)],
        BenchmarkScenarioId.Collections => [typeof(CollectionAggregationBenchmarks)],
        BenchmarkScenarioId.Json => GetJsonBenchmarkTypes(jsonGroup),
        BenchmarkScenarioId.AsyncIo => [typeof(AsyncIoBatchBenchmarks)],
        _ => throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unsupported scenario.")
    };

    private static IReadOnlyList<Type> GetJsonBenchmarkTypes(JsonBenchmarkGroup jsonGroup) => jsonGroup switch
    {
        JsonBenchmarkGroup.All => [typeof(JsonPayloadParsingBenchmarks), typeof(JsonPayloadStreamingParsingBenchmarks), typeof(JsonPayloadEndToEndBenchmarks), typeof(JsonPayloadStreamingEndToEndBenchmarks)],
        JsonBenchmarkGroup.ParserOnly => [typeof(JsonPayloadParsingBenchmarks), typeof(JsonPayloadStreamingParsingBenchmarks)],
        JsonBenchmarkGroup.EndToEnd => [typeof(JsonPayloadEndToEndBenchmarks), typeof(JsonPayloadStreamingEndToEndBenchmarks)],
        _ => throw new ArgumentOutOfRangeException(nameof(jsonGroup), jsonGroup, "Unsupported JSON benchmark group.")
    };
}
