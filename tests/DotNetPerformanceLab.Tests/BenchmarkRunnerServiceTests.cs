using DotNetPerformanceLab.Benchmarking;
using BenchmarkDotNet.Reports;

namespace DotNetPerformanceLab.Tests;

public class BenchmarkRunnerServiceTests
{
    private static readonly object WorkingDirectoryLock = new();

    [Fact]
    public void GetRecentReportFiles_NoReports_ReturnsEmpty()
    {
        lock (WorkingDirectoryLock)
        {
            string originalDirectory = Environment.CurrentDirectory;
            string tempRoot = Path.Combine(Path.GetTempPath(), "DotNetPerformanceLab.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                Environment.CurrentDirectory = tempRoot;
                BenchmarkRunnerService service = new();

                IReadOnlyList<string> reports = service.GetRecentReportFiles();

                Assert.Empty(reports);
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void GetRecentReportFiles_ReturnsSortedReportFiles()
    {
        lock (WorkingDirectoryLock)
        {
            string originalDirectory = Environment.CurrentDirectory;
            string tempRoot = Path.Combine(Path.GetTempPath(), "DotNetPerformanceLab.Tests", Guid.NewGuid().ToString("N"));
            string reportsRoot = Path.Combine(tempRoot, "BenchmarkDotNet.Artifacts", "results");
            Directory.CreateDirectory(reportsRoot);

            string older = Path.Combine(reportsRoot, "A-report.csv");
            string newer = Path.Combine(reportsRoot, "B-report.html");
            string ignored = Path.Combine(reportsRoot, "ignored.txt");

            try
            {
                File.WriteAllText(older, "old");
                File.WriteAllText(newer, "new");
                File.WriteAllText(ignored, "ignore");

                File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-10));
                File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);

                Environment.CurrentDirectory = tempRoot;
                BenchmarkRunnerService service = new();
                IReadOnlyList<string> reports = service.GetRecentReportFiles(maxCount: 10);

                Assert.Equal(2, reports.Count);
                Assert.Equal(newer, reports[0]);
                Assert.Equal(older, reports[1]);
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void Run_PreflightFailure_DoesNotExecuteBenchmarks()
    {
        FakeBenchmarkExecutor executor = new();
        BenchmarkRunnerService service = new(
            preflightService: new StubPreflightService(success: false),
            preflightRenderer: new NullPreflightRenderer(),
            benchmarkExecutor: executor,
            performanceLabSummaryRenderer: new NullPerformanceLabSummaryRenderer());

        int exitCode = service.Run(CreateOptions(BenchmarkScenarioId.Collections));

        Assert.Equal(1, exitCode);
        Assert.False(executor.WasCalled);
    }

    [Fact]
    public void Run_PreflightSuccess_ExecutesBenchmarks()
    {
        FakeBenchmarkExecutor executor = new();
        BenchmarkRunnerService service = new(
            preflightService: new StubPreflightService(success: true),
            preflightRenderer: new NullPreflightRenderer(),
            benchmarkExecutor: executor,
            performanceLabSummaryRenderer: new NullPerformanceLabSummaryRenderer());

        int exitCode = service.Run(CreateOptions(BenchmarkScenarioId.Collections));

        Assert.Equal(0, exitCode);
        Assert.True(executor.WasCalled);
    }

    private static BenchmarkRunOptions CreateOptions(BenchmarkScenarioId scenarioId) => new(
        IsBenchmarkMode: true,
        RunAllScenarios: false,
        ScenarioId: scenarioId,
        DatasetSize: DatasetSize.Small,
        JsonGroup: JsonBenchmarkGroup.All,
        IsHelpRequested: false,
        SkipPreflight: false);

    private sealed class NullPreflightRenderer : IBenchmarkPreflightRenderer
    {
        public void Render(BenchmarkPreflightResult result)
        {
        }
    }

    private sealed class StubPreflightService : IBenchmarkPreflightService
    {
        private readonly bool _success;

        public StubPreflightService(bool success)
        {
            _success = success;
        }

        public BenchmarkPreflightResult Validate(BenchmarkRunOptions options)
        {
            ScenarioPreflightResult scenarioResult = new(
                options.ScenarioId ?? BenchmarkScenarioId.Collections,
                "Stub",
                1,
                _success ? ScenarioPreflightStatus.Passed : ScenarioPreflightStatus.Failed,
                _success ? "ok" : "mismatch",
                []);

            return new BenchmarkPreflightResult(options.DatasetSize, [scenarioResult]);
        }
    }

    private sealed class FakeBenchmarkExecutor : IBenchmarkExecutor
    {
        public bool WasCalled { get; private set; }

        public IReadOnlyList<Summary> Run(IReadOnlyList<Type> benchmarkTypes)
        {
            WasCalled = true;
            return [];
        }
    }

    private sealed class NullPerformanceLabSummaryRenderer : IPerformanceLabSummaryRenderer
    {
        public void Render(PerformanceLabSummaryRun summary)
        {
        }
    }
}
