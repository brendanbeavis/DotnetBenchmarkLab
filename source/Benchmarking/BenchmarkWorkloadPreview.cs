namespace DotNetPerformanceLab.Benchmarking;

public sealed record BenchmarkWorkloadPreview(
    string Title,
    DatasetSize DatasetSize,
    bool IsRunAll,
    IReadOnlyList<ScenarioWorkloadPreview> Scenarios);

public sealed record ScenarioWorkloadPreview(
    BenchmarkScenarioId ScenarioId,
    string ScenarioName,
    string BusinessProblem,
    IReadOnlyList<(string Label, string Value)> InputDataset,
    IReadOnlyList<string> WorkloadShape,
    IReadOnlyList<BenchmarkMethodPreview> MethodsCompared,
    IReadOnlyList<string> Included,
    IReadOnlyList<string> Excluded,
    IReadOnlyList<string> ExpectedConsiderations,
    IReadOnlyList<(string Label, string Value)> RunSettings);

public sealed record BenchmarkMethodPreview(string MethodName, string Description, string TradeoffNote);
