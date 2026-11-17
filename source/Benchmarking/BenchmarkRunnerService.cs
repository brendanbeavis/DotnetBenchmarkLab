using BenchmarkDotNet.Running;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class BenchmarkRunnerService
{
    public int Run(BenchmarkRunOptions options)
    {
        BenchmarkRuntimeContext.DatasetSize = options.DatasetSize;
        BenchmarkRuntimeContext.JsonBenchmarkGroup = options.JsonGroup;

        IReadOnlyList<Type> benchmarkTypes = options.RunAllScenarios
            ? BenchmarkTypeRegistry.GetAllBenchmarkTypes()
            : options.ScenarioId is { } scenarioId
                ? BenchmarkTypeRegistry.GetBenchmarkTypes(scenarioId, options.JsonGroup)
                : [];

        if (benchmarkTypes.Count == 0)
        {
            return 1;
        }

        BenchmarkRunner.Run(benchmarkTypes.ToArray(), BenchmarkConfigFactory.Create());
        ArchiveReportSnapshots();
        return 0;
    }

    public IReadOnlyList<string> GetRecentReportFiles(int maxCount = 10)
    {
        string[] reportDirectories = BenchmarkConventions
            .GetCandidateReportsDirectories()
            .Where(Directory.Exists)
            .ToArray();

        if (reportDirectories.Length == 0)
        {
            return [];
        }

        return reportDirectories
            .SelectMany(directory => Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly))
            .Where(file => file.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(maxCount)
            .ToArray();
    }

    private static void ArchiveReportSnapshots()
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        foreach (string reportsDirectory in BenchmarkConventions.GetCandidateReportsDirectories().Where(Directory.Exists))
        {
            string[] reportFiles = Directory
                .GetFiles(reportsDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => file.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (reportFiles.Length == 0)
            {
                continue;
            }

            string historyDirectory = Path.Combine(reportsDirectory, "history");
            Directory.CreateDirectory(historyDirectory);

            foreach (string sourceFile in reportFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(sourceFile);
                string extension = Path.GetExtension(sourceFile);
                string destinationFile = Path.Combine(historyDirectory, $"{fileName}-{timestamp}{extension}");
                File.Copy(sourceFile, destinationFile, overwrite: true);
            }
        }
    }
}
