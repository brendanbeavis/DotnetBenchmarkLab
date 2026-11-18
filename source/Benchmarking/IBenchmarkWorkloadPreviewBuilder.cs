namespace DotNetPerformanceLab.Benchmarking;

public interface IBenchmarkWorkloadPreviewBuilder
{
    BenchmarkWorkloadPreview Build(BenchmarkRunOptions options);
}
