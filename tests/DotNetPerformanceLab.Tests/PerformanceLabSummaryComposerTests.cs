using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Tests;

public class PerformanceLabSummaryComposerTests
{
    [Fact]
    public void Compose_ComputesBaselineRelativeRatios()
    {
        BenchmarkSummaryMeasurement baseline = CreateMeasurement("A", isBaseline: true, meanNs: 1000, allocatedBytes: 100);
        BenchmarkSummaryMeasurement candidate = CreateMeasurement("B", isBaseline: false, meanNs: 500, allocatedBytes: 50);

        PerformanceLabSummaryRun summary = PerformanceLabSummaryComposer.Compose(
            runId: "run-1",
            generatedAtUtc: DateTime.UtcNow,
            datasetSize: DatasetSize.Small,
            runLabel: "test",
            preflightStatus: PreflightSummaryStatus.Passed,
            preflightMessage: "ok",
            measurements: [baseline, candidate]);

        PerformanceLabSummaryGroup group = Assert.Single(summary.Groups);
        PerformanceLabSummaryEntry candidateEntry = group.Entries.Single(x => x.Method == "B");

        Assert.Equal(0.5d, candidateEntry.RelativeTimeRatio);
        Assert.Equal(0.5d, candidateEntry.RelativeAllocationRatio);
    }

    [Fact]
    public void Compose_MissingBaseline_ReportsMissingData()
    {
        BenchmarkSummaryMeasurement a = CreateMeasurement("A", isBaseline: false, meanNs: 1000, allocatedBytes: 100);
        BenchmarkSummaryMeasurement b = CreateMeasurement("B", isBaseline: false, meanNs: 900, allocatedBytes: 80);

        PerformanceLabSummaryRun summary = PerformanceLabSummaryComposer.Compose(
            runId: "run-1",
            generatedAtUtc: DateTime.UtcNow,
            datasetSize: DatasetSize.Small,
            runLabel: "test",
            preflightStatus: PreflightSummaryStatus.Passed,
            preflightMessage: "ok",
            measurements: [a, b]);

        PerformanceLabSummaryGroup group = Assert.Single(summary.Groups);

        Assert.True(group.HasMissingData);
        Assert.Contains("No explicit baseline", group.MissingDataMessage, StringComparison.Ordinal);
        Assert.All(group.Entries, entry => Assert.Null(entry.RelativeTimeRatio));
    }

    [Fact]
    public void DetermineClarity_OverlappingIntervals_NoClearDifference()
    {
        BenchmarkSummaryMeasurement baseline = CreateMeasurement("A", isBaseline: true, meanNs: 1000, stdDevNs: 100, sampleCount: 20, allocatedBytes: 100);
        BenchmarkSummaryMeasurement candidate = CreateMeasurement("B", isBaseline: false, meanNs: 1020, stdDevNs: 110, sampleCount: 20, allocatedBytes: 100);

        ComparisonClarity clarity = PerformanceLabSummaryComposer.DetermineClarity(baseline, candidate);

        Assert.Equal(ComparisonClarity.NoClearDifference, clarity);
    }

    [Fact]
    public void DetermineClarity_SeparatedIntervals_ClearDifference()
    {
        BenchmarkSummaryMeasurement baseline = CreateMeasurement("A", isBaseline: true, meanNs: 1000, stdDevNs: 20, sampleCount: 30, allocatedBytes: 100);
        BenchmarkSummaryMeasurement candidate = CreateMeasurement("B", isBaseline: false, meanNs: 1300, stdDevNs: 20, sampleCount: 30, allocatedBytes: 120);

        ComparisonClarity clarity = PerformanceLabSummaryComposer.DetermineClarity(baseline, candidate);

        Assert.Equal(ComparisonClarity.ClearDifference, clarity);
    }

    [Fact]
    public void Compose_MultipleGroups_AreSeparated()
    {
        BenchmarkSummaryMeasurement dbA = CreateMeasurement("A", true, 1000, 100, scenario: "Database", benchmarkGroup: "FullEntity", benchmarkType: "DbBench");
        BenchmarkSummaryMeasurement dbB = CreateMeasurement("B", false, 900, 90, scenario: "Database", benchmarkGroup: "FullEntity", benchmarkType: "DbBench");
        BenchmarkSummaryMeasurement colA = CreateMeasurement("A", true, 2000, 200, scenario: "Collections", benchmarkGroup: "All", benchmarkType: "ColBench");
        BenchmarkSummaryMeasurement colB = CreateMeasurement("B", false, 1500, 150, scenario: "Collections", benchmarkGroup: "All", benchmarkType: "ColBench");

        PerformanceLabSummaryRun summary = PerformanceLabSummaryComposer.Compose(
            runId: "run-1",
            generatedAtUtc: DateTime.UtcNow,
            datasetSize: DatasetSize.Small,
            runLabel: "test",
            preflightStatus: PreflightSummaryStatus.Passed,
            preflightMessage: "ok",
            measurements: [dbA, dbB, colA, colB]);

        Assert.Equal(2, summary.Groups.Count);
        Assert.Contains(summary.Groups, x => x.Scenario == "Database");
        Assert.Contains(summary.Groups, x => x.Scenario == "Collections");
    }

    private static BenchmarkSummaryMeasurement CreateMeasurement(
        string method,
        bool isBaseline,
        double meanNs,
        double allocatedBytes,
        double? stdDevNs = 10,
        double? sampleCount = 20,
        string scenario = "Json",
        string benchmarkGroup = "ParserOnly",
        string benchmarkType = "JsonBench")
    {
        return new BenchmarkSummaryMeasurement(
            scenario,
            benchmarkGroup,
            benchmarkType,
            "env",
            method,
            "Basic",
            isBaseline,
            MeanNanoseconds: meanNs,
            ErrorNanoseconds: stdDevNs.HasValue && sampleCount.HasValue ? stdDevNs.Value / Math.Sqrt(sampleCount.Value) : null,
            StdDevNanoseconds: stdDevNs,
            SampleCount: sampleCount,
            AllocatedBytes: allocatedBytes);
    }
}
