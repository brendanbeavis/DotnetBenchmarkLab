namespace DotNetPerformanceLab.Benchmarking;

public interface IBenchmarkPreflightService
{
    BenchmarkPreflightResult Validate(BenchmarkRunOptions options);
}
