using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class BenchmarkDotNetExecutor : IBenchmarkExecutor
{
    public IReadOnlyList<Summary> Run(IReadOnlyList<Type> benchmarkTypes)
    {
        return BenchmarkRunner.Run(benchmarkTypes.ToArray(), BenchmarkConfigFactory.Create());
    }
}
