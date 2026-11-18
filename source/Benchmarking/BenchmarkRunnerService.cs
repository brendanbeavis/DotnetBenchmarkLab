namespace DotNetPerformanceLab.Benchmarking;

public sealed class BenchmarkRunnerService
{
    private readonly IBenchmarkPreflightService _preflightService;
    private readonly IBenchmarkPreflightRenderer _preflightRenderer;
    private readonly IBenchmarkExecutor _benchmarkExecutor;
    private readonly IPerformanceLabSummaryRenderer _performanceLabSummaryRenderer;

    public BenchmarkRunnerService(
        IBenchmarkPreflightService? preflightService = null,
        IBenchmarkPreflightRenderer? preflightRenderer = null,
        IBenchmarkExecutor? benchmarkExecutor = null,
        IPerformanceLabSummaryRenderer? performanceLabSummaryRenderer = null)
    {
        _preflightService = preflightService ?? new BenchmarkPreflightService();
        _preflightRenderer = preflightRenderer ?? new SpectreBenchmarkPreflightRenderer();
        _benchmarkExecutor = benchmarkExecutor ?? new BenchmarkDotNetExecutor();
        _performanceLabSummaryRenderer = performanceLabSummaryRenderer ?? new SpectrePerformanceLabSummaryRenderer();
    }

    public int Run(BenchmarkRunOptions options)
    {
        BenchmarkPreflightResult? preflightResult = null;
        if (!options.SkipPreflight)
        {
            preflightResult = _preflightService.Validate(options);
            _preflightRenderer.Render(preflightResult);
            if (!preflightResult.IsSuccess)
            {
                return 1;
            }
        }

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

        DateTime runUtc = DateTime.UtcNow;
        string runId = runUtc.ToString("yyyyMMdd-HHmmss");

        IReadOnlyList<BenchmarkDotNet.Reports.Summary> summaries = _benchmarkExecutor.Run(benchmarkTypes);
        IReadOnlyList<BenchmarkSummaryMeasurement> measurements = BenchmarkDotNetSummaryMeasurementExtractor.Extract(summaries);

        (PreflightSummaryStatus status, string message) = ResolvePreflightStatus(options, preflightResult);
        string runLabel = options.RunAllScenarios
            ? "All benchmark scenarios"
            : options.ScenarioId?.ToString() ?? "Unknown scenario";

        PerformanceLabSummaryRun performanceSummary = PerformanceLabSummaryComposer.Compose(
            runId,
            runUtc,
            options.DatasetSize,
            runLabel,
            status,
            message,
            measurements);

        _performanceLabSummaryRenderer.Render(performanceSummary);
        ArchiveReportSnapshots();
        return 0;
    }

    private static (PreflightSummaryStatus status, string message) ResolvePreflightStatus(BenchmarkRunOptions options, BenchmarkPreflightResult? preflightResult)
    {
        if (options.SkipPreflight)
        {
            return (PreflightSummaryStatus.Skipped, "Preflight skipped in this process (interactive parent process may have already validated correctness).");
        }

        if (preflightResult is null)
        {
            return (PreflightSummaryStatus.Unknown, "No preflight result was captured.");
        }

        if (preflightResult.IsSuccess)
        {
            return (PreflightSummaryStatus.Passed, "All compared implementations were equivalent in correctness preflight.");
        }

        return (PreflightSummaryStatus.Failed, "One or more scenario validations failed.");
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
