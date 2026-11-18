namespace DotNetPerformanceLab.Benchmarking;

public static class PerformanceLabSummaryComposer
{
    public static PerformanceLabSummaryRun Compose(
        string runId,
        DateTime generatedAtUtc,
        DatasetSize datasetSize,
        string runLabel,
        PreflightSummaryStatus preflightStatus,
        string preflightMessage,
        IReadOnlyList<BenchmarkSummaryMeasurement> measurements)
    {
        IReadOnlyList<PerformanceLabSummaryGroup> groups = measurements
            .GroupBy(x => (x.Scenario, x.BenchmarkGroup, x.BenchmarkType, x.Environment))
            .Select(BuildGroup)
            .OrderBy(x => x.Scenario, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.BenchmarkGroup, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.BenchmarkType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PerformanceLabSummaryRun(
            runId,
            generatedAtUtc,
            datasetSize,
            runLabel,
            preflightStatus,
            preflightMessage,
            groups);
    }

    private static PerformanceLabSummaryGroup BuildGroup(IGrouping<(string Scenario, string BenchmarkGroup, string BenchmarkType, string Environment), BenchmarkSummaryMeasurement> grouping)
    {
        BenchmarkSummaryMeasurement[] rows = grouping.ToArray();
        BenchmarkSummaryMeasurement? baseline = ResolveBaseline(rows);

        bool hasMissingData = false;
        List<string> missingMessages = [];

        if (baseline is null)
        {
            hasMissingData = true;
            missingMessages.Add("No explicit baseline found; baseline-relative ratios are unavailable.");
        }

        List<PerformanceLabSummaryEntry> entries = [];
        foreach (BenchmarkSummaryMeasurement row in rows.OrderBy(x => x.Method, StringComparer.Ordinal))
        {
            double? timeRatio = baseline is not null && row.MeanNanoseconds.HasValue && baseline.MeanNanoseconds.HasValue && baseline.MeanNanoseconds.Value > 0
                ? row.MeanNanoseconds.Value / baseline.MeanNanoseconds.Value
                : null;

            double? allocationRatio = baseline is not null && row.AllocatedBytes.HasValue && baseline.AllocatedBytes.HasValue && baseline.AllocatedBytes.Value > 0
                ? row.AllocatedBytes.Value / baseline.AllocatedBytes.Value
                : null;

            ComparisonClarity clarity = DetermineClarity(baseline, row);
            if (row.MeanNanoseconds is null)
            {
                hasMissingData = true;
                missingMessages.Add($"Missing mean time for {row.Method}.");
            }
            if (row.AllocatedBytes is null)
            {
                hasMissingData = true;
                missingMessages.Add($"Missing allocated bytes for {row.Method}.");
            }

            entries.Add(new PerformanceLabSummaryEntry(
                row.Method,
                row.ImplementationLevel,
                row.IsBaseline,
                row.MeanNanoseconds,
                row.ErrorNanoseconds,
                row.StdDevNanoseconds,
                row.SampleCount,
                row.AllocatedBytes,
                timeRatio,
                allocationRatio,
                clarity));
        }

        string interpretation = BuildInterpretation(entries, baseline);

        return new PerformanceLabSummaryGroup(
            grouping.Key.Scenario,
            grouping.Key.BenchmarkGroup,
            grouping.Key.BenchmarkType,
            grouping.Key.Environment,
            entries,
            hasMissingData,
            string.Join(" ", missingMessages.Distinct(StringComparer.Ordinal)),
            interpretation);
    }

    private static BenchmarkSummaryMeasurement? ResolveBaseline(IReadOnlyList<BenchmarkSummaryMeasurement> rows)
    {
        BenchmarkSummaryMeasurement[] baselines = rows.Where(x => x.IsBaseline).ToArray();
        if (baselines.Length == 1)
        {
            return baselines[0];
        }

        if (baselines.Length > 1)
        {
            return baselines.OrderBy(x => x.Method, StringComparer.Ordinal).First();
        }

        return null;
    }

    public static ComparisonClarity DetermineClarity(BenchmarkSummaryMeasurement? baseline, BenchmarkSummaryMeasurement candidate)
    {
        if (baseline is null || candidate.Method == baseline.Method)
        {
            return ComparisonClarity.Unknown;
        }

        if (!TryGetConfidenceInterval(baseline, out (double min, double max) baselineCi)
            || !TryGetConfidenceInterval(candidate, out (double min, double max) candidateCi))
        {
            return ComparisonClarity.Unknown;
        }

        bool overlap = baselineCi.min <= candidateCi.max && candidateCi.min <= baselineCi.max;
        return overlap ? ComparisonClarity.NoClearDifference : ComparisonClarity.ClearDifference;
    }

    private static bool TryGetConfidenceInterval(BenchmarkSummaryMeasurement measurement, out (double min, double max) interval)
    {
        interval = default;
        if (!measurement.MeanNanoseconds.HasValue || !measurement.StdDevNanoseconds.HasValue || !measurement.SampleCount.HasValue)
        {
            return false;
        }

        double n = measurement.SampleCount.Value;
        if (n <= 1)
        {
            return false;
        }

        double standardError = measurement.StdDevNanoseconds.Value / Math.Sqrt(n);
        double margin = 1.96d * standardError;
        interval = (measurement.MeanNanoseconds.Value - margin, measurement.MeanNanoseconds.Value + margin);
        return true;
    }

    private static string BuildInterpretation(IReadOnlyList<PerformanceLabSummaryEntry> entries, BenchmarkSummaryMeasurement? baseline)
    {
        if (baseline is null)
        {
            return "No explicit baseline was available, so relative winner statements are not produced.";
        }

        PerformanceLabSummaryEntry? fastest = entries
            .Where(x => x.MeanNanoseconds.HasValue)
            .OrderBy(x => x.MeanNanoseconds)
            .FirstOrDefault();

        if (fastest is null)
        {
            return "No complete timing data was available for interpretation.";
        }

        if (fastest.Method == baseline.Method)
        {
            return "Baseline was the fastest measured method in this group.";
        }

        if (fastest.TimeClarity == ComparisonClarity.NoClearDifference)
        {
            return "No clear difference measured versus baseline based on overlapping confidence estimates.";
        }

        if (!fastest.RelativeTimeRatio.HasValue || fastest.RelativeTimeRatio.Value <= 0)
        {
            return "Relative timing ratio was unavailable for the fastest measured method.";
        }

        double speedup = 1d / fastest.RelativeTimeRatio.Value;
        return $"Largest observed time difference: {fastest.Method} at {speedup:0.##}x versus baseline. This benchmark does not prove production-wide end-to-end impact.";
    }
}
