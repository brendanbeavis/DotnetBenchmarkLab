namespace DotNetPerformanceLab.Benchmarking;

public sealed record BenchmarkScenario(
    BenchmarkScenarioId Id,
    string DisplayName,
    string Description,
    string[] LearningObjectives,
    bool IsImplemented,
    string StatusNote);
