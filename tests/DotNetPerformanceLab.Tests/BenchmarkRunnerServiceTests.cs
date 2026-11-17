using DotNetPerformanceLab.Benchmarking;

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
}
