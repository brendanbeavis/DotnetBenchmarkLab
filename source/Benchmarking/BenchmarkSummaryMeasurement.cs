namespace DotNetPerformanceLab.Benchmarking;

public sealed record BenchmarkSummaryMeasurement(
    string Scenario,
    string BenchmarkGroup,
    string BenchmarkType,
    string Environment,
    string Method,
    string ImplementationLevel,
    bool IsBaseline,
    double? MeanNanoseconds,
    double? ErrorNanoseconds,
    double? StdDevNanoseconds,
    double? SampleCount,
    double? AllocatedBytes);
