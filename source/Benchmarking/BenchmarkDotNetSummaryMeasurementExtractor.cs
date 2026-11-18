using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;

namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkDotNetSummaryMeasurementExtractor
{
    public static IReadOnlyList<BenchmarkSummaryMeasurement> Extract(IReadOnlyList<Summary> summaries)
    {
        List<BenchmarkSummaryMeasurement> measurements = [];

        foreach (Summary summary in summaries)
        {
            string environment = BuildEnvironmentLabel(summary);

            foreach (BenchmarkReport report in summary.Reports)
            {
                string[] categories = report.BenchmarkCase.Descriptor.Categories;
                string scenario = ResolveScenario(categories, report.BenchmarkCase.Descriptor.Type);
                string implementationLevel = ResolveImplementationLevel(categories);
                string benchmarkGroup = ResolveBenchmarkGroup(categories, summary.Title);
                string benchmarkType = report.BenchmarkCase.Descriptor.Type.Name;
                string method = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
                bool isBaseline = report.BenchmarkCase.Descriptor.WorkloadMethod
                    .GetCustomAttributes(typeof(BenchmarkAttribute), inherit: true)
                    .OfType<BenchmarkAttribute>()
                    .Any(x => x.Baseline);

                object? stats = report.ResultStatistics;
                object? gcStats = report.GcStats;

                measurements.Add(new BenchmarkSummaryMeasurement(
                    scenario,
                    benchmarkGroup,
                    benchmarkType,
                    environment,
                    method,
                    implementationLevel,
                    isBaseline,
                    MeanNanoseconds: TryGetDouble(stats, "Mean"),
                    ErrorNanoseconds: TryGetDouble(stats, "StandardError", "Error"),
                    StdDevNanoseconds: TryGetDouble(stats, "StandardDeviation"),
                    SampleCount: TryGetDouble(stats, "N"),
                    AllocatedBytes: TryGetDouble(gcStats, "BytesAllocatedPerOperation")));
            }
        }

        return measurements;
    }

    private static string ResolveScenario(string[] categories, Type benchmarkType)
    {
        if (categories.Any(x => string.Equals(x, "Database", StringComparison.OrdinalIgnoreCase)))
        {
            return "Database";
        }

        if (categories.Any(x => string.Equals(x, "Collections", StringComparison.OrdinalIgnoreCase)))
        {
            return "Collections";
        }

        if (categories.Any(x => string.Equals(x, "Json", StringComparison.OrdinalIgnoreCase)))
        {
            return "Json";
        }

        if (categories.Any(x => string.Equals(x, "AsyncIo", StringComparison.OrdinalIgnoreCase)))
        {
            return "AsyncIo";
        }

        string @namespace = benchmarkType.Namespace ?? string.Empty;
        if (@namespace.Contains(".Database", StringComparison.Ordinal))
        {
            return "Database";
        }

        if (@namespace.Contains(".Collections", StringComparison.Ordinal))
        {
            return "Collections";
        }

        if (@namespace.Contains(".Json", StringComparison.Ordinal))
        {
            return "Json";
        }

        if (@namespace.Contains(".AsyncIo", StringComparison.Ordinal))
        {
            return "AsyncIo";
        }

        return "Unknown";
    }

    private static string ResolveImplementationLevel(string[] categories)
    {
        if (categories.Any(x => string.Equals(x, "Basic", StringComparison.OrdinalIgnoreCase)))
        {
            return "Basic";
        }

        if (categories.Any(x => string.Equals(x, "Intermediate", StringComparison.OrdinalIgnoreCase)))
        {
            return "Intermediate";
        }

        if (categories.Any(x => string.Equals(x, "Advanced", StringComparison.OrdinalIgnoreCase)))
        {
            return "Advanced";
        }

        return "Unspecified";
    }

    private static string ResolveBenchmarkGroup(string[] categories, string summaryTitle)
    {
        string[] groupCategories = categories
            .Where(x => !string.Equals(x, "Database", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x, "Collections", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x, "Json", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x, "AsyncIo", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x, "Basic", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x, "Intermediate", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(x, "Advanced", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (groupCategories.Length > 0)
        {
            return string.Join(" + ", groupCategories.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        }

        return summaryTitle;
    }

    private static string BuildEnvironmentLabel(Summary summary)
    {
        object host = summary.HostEnvironmentInfo;
        string runtime = TryGetString(host, "BenchmarkDotNetCaption", "RuntimeVersion", "Runtime") ?? "unknown runtime";
        string os = TryGetString(host, "OsVersion", "Os", "OS") ?? "unknown OS";
        string architecture = TryGetString(host, "Architecture") ?? "unknown arch";
        string processor = TryGetString(host, "ProcessorName", "CpuInfo") ?? "unknown CPU";

        return $"{runtime} | {os} | {architecture} | {processor}";
    }

    private static double? TryGetDouble(object? source, params string[] propertyNames)
    {
        if (source is null)
        {
            return null;
        }

        foreach (string propertyName in propertyNames)
        {
            object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is null)
            {
                continue;
            }

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ToDouble(value);
                }
                catch
                {
                    // ignore conversion and try next
                }
            }
        }

        return null;
    }

    private static string? TryGetString(object source, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is null)
            {
                continue;
            }

            string text = value.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }
}
