namespace DotNetPerformanceLab.Benchmarking;

public sealed record BenchmarkRunOptions(
    bool IsBenchmarkMode,
    bool RunAllScenarios,
    BenchmarkScenarioId? ScenarioId,
    DatasetSize DatasetSize,
    JsonBenchmarkGroup JsonGroup,
    bool IsHelpRequested,
    bool SkipPreflight)
{
    public static BenchmarkRunOptions InteractiveDefault { get; } =
        new(IsBenchmarkMode: false, RunAllScenarios: false, ScenarioId: null, DatasetSize.Medium, JsonBenchmarkGroup.All, IsHelpRequested: false, SkipPreflight: false);
}
