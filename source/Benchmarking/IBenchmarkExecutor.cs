using BenchmarkDotNet.Reports;

namespace DotNetPerformanceLab.Benchmarking;

public interface IBenchmarkExecutor
{
    IReadOnlyList<Summary> Run(IReadOnlyList<Type> benchmarkTypes);
}
