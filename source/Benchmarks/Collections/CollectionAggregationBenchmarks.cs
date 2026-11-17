using BenchmarkDotNet.Attributes;
using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Models;
using DotNetPerformanceLab.Reporting.Collections;

namespace DotNetPerformanceLab.Benchmarks.Collections;

[MemoryDiagnoser]
public class CollectionAggregationBenchmarks
{
    [ParamsSource(nameof(DatasetSizes))]
    public string Dataset { get; set; } = nameof(DatasetSize.Medium);

    public IEnumerable<string> DatasetSizes => [BenchmarkConventions.ToParameterValue(BenchmarkRuntimeContext.DatasetSize)];

    private IReadOnlyList<RetailTransaction> _transactions = [];
    private CollectionsAggregationService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        DatasetSize datasetSize = Enum.Parse<DatasetSize>(Dataset, ignoreCase: true);
        CollectionDatasetFixture fixture = CollectionDatasetGenerator.Create(datasetSize);
        _transactions = fixture.Transactions;
        _service = new CollectionsAggregationService();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Collections", "Basic")]
    public SalesSummary Linq()
    {
        return _service.AggregateWithLinq(_transactions);
    }

    [Benchmark]
    [BenchmarkCategory("Collections", "Intermediate")]
    public SalesSummary Foreach()
    {
        return _service.AggregateWithForeach(_transactions);
    }

    [Benchmark]
    [BenchmarkCategory("Collections", "Intermediate")]
    public SalesSummary DictionaryAggregation()
    {
        return _service.AggregateWithDictionary(_transactions);
    }

    [Benchmark]
    [BenchmarkCategory("Collections", "Advanced")]
    public SalesSummary ParallelFor()
    {
        return _service.AggregateWithParallelFor(_transactions);
    }

    [Benchmark]
    [BenchmarkCategory("Collections", "Advanced")]
    public SalesSummary Plinq()
    {
        return _service.AggregateWithPlinq(_transactions);
    }
}
