namespace DotNetPerformanceLab.Benchmarking;

public sealed record BenchmarkPreflightResult(
    DatasetSize DatasetSize,
    IReadOnlyList<ScenarioPreflightResult> ScenarioResults)
{
    public bool IsSuccess => ScenarioResults.Count > 0 && ScenarioResults.All(x => x.Status == ScenarioPreflightStatus.Passed || x.Status == ScenarioPreflightStatus.Skipped);
}

public sealed record ScenarioPreflightResult(
    BenchmarkScenarioId ScenarioId,
    string ScenarioName,
    int ApproximateRecordCount,
    ScenarioPreflightStatus Status,
    string Message,
    IReadOnlyList<ImplementationPreflightResult> ImplementationsChecked);

public sealed record ImplementationPreflightResult(
    string Name,
    bool Passed,
    string Detail);

public enum ScenarioPreflightStatus
{
    Passed,
    Failed,
    Skipped
}
