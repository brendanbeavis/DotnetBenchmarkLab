namespace DotNetPerformanceLab.Benchmarking;

public sealed record PerformanceLabSummaryRun(
    string RunId,
    DateTime GeneratedAtUtc,
    DatasetSize DatasetSize,
    string RunLabel,
    PreflightSummaryStatus PreflightStatus,
    string PreflightMessage,
    IReadOnlyList<PerformanceLabSummaryGroup> Groups);

public sealed record PerformanceLabSummaryGroup(
    string Scenario,
    string BenchmarkGroup,
    string BenchmarkType,
    string Environment,
    IReadOnlyList<PerformanceLabSummaryEntry> Entries,
    bool HasMissingData,
    string MissingDataMessage,
    string Interpretation);

public sealed record PerformanceLabSummaryEntry(
    string Method,
    string ImplementationLevel,
    bool IsBaseline,
    double? MeanNanoseconds,
    double? ErrorNanoseconds,
    double? StdDevNanoseconds,
    double? SampleCount,
    double? AllocatedBytes,
    double? RelativeTimeRatio,
    double? RelativeAllocationRatio,
    ComparisonClarity TimeClarity);

public enum ComparisonClarity
{
    Unknown,
    ClearDifference,
    NoClearDifference
}

public enum PreflightSummaryStatus
{
    Passed,
    Failed,
    Skipped,
    Unknown
}
