namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkRuntimeContext
{
    public static DatasetSize DatasetSize { get; set; } = DatasetSize.Medium;

    public static JsonBenchmarkGroup JsonBenchmarkGroup { get; set; } = JsonBenchmarkGroup.All;
}
