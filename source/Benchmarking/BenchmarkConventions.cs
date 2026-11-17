using System.Reflection;

namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkConventions
{
    public const int DeterministicSeed = 424242;

    public static string ReportsRootDirectory => GetCandidateReportsDirectories().First();

    public static int GetApproximateRecordCount(DatasetSize datasetSize) => datasetSize switch
    {
        DatasetSize.Small => 1_000,
        DatasetSize.Medium => 10_000,
        DatasetSize.Large => 100_000,
        _ => throw new ArgumentOutOfRangeException(nameof(datasetSize), datasetSize, "Unsupported dataset size.")
    };

    public static string ToParameterValue(DatasetSize datasetSize) => datasetSize.ToString();

    public static string GetExecutablePath() => Assembly.GetExecutingAssembly().Location;

    public static IReadOnlyList<string> GetCandidateReportsDirectories()
    {
        string cwdReports = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "BenchmarkDotNet.Artifacts", "results"));
        string appBaseReports = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts", "results"));

        return new[] { cwdReports, appBaseReports }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
