using BenchmarkDotNet.Attributes;
using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.AsyncIo;

namespace DotNetPerformanceLab.Benchmarks.AsyncIo;

[MemoryDiagnoser]
public class AsyncIoBatchBenchmarks
{
    [ParamsSource(nameof(DatasetSizes))]
    public string Dataset { get; set; } = nameof(DatasetSize.Medium);

    public IEnumerable<string> DatasetSizes => [BenchmarkConventions.ToParameterValue(BenchmarkRuntimeContext.DatasetSize)];

    private AsyncIoBatchFixture _fixture = null!;
    private AsyncIoBatchProcessingService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        DatasetSize datasetSize = Enum.Parse<DatasetSize>(Dataset, ignoreCase: true);
        _fixture = AsyncIoBatchFixtureFactory.CreateAsync(datasetSize).GetAwaiter().GetResult();
        _service = new AsyncIoBatchProcessingService();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("AsyncIo", "Basic", "BatchHttp")]
    public AsyncIoBatchProcessingResult SequentialAsync()
    {
        return _service.RunSequentialAsync(_fixture.HttpClient, _fixture.Server, _fixture.RequestBatch).GetAwaiter().GetResult();
    }

    [Benchmark]
    [BenchmarkCategory("AsyncIo", "Intermediate", "BatchHttp")]
    public AsyncIoBatchProcessingResult TaskWhenAll()
    {
        return _service.RunTaskWhenAllAsync(_fixture.HttpClient, _fixture.Server, _fixture.RequestBatch).GetAwaiter().GetResult();
    }

    [Benchmark]
    [BenchmarkCategory("AsyncIo", "Advanced", "BatchHttp")]
    public AsyncIoBatchProcessingResult SemaphoreSlimBounded()
    {
        return _service.RunSemaphoreBoundedAsync(
            _fixture.HttpClient,
            _fixture.Server,
            _fixture.RequestBatch,
            _fixture.Profile.BoundedConcurrencyLimit).GetAwaiter().GetResult();
    }

    [Benchmark]
    [BenchmarkCategory("AsyncIo", "Advanced", "BatchHttp")]
    public AsyncIoBatchProcessingResult ParallelForEachAsyncBounded()
    {
        return _service.RunParallelForEachAsync(
            _fixture.HttpClient,
            _fixture.Server,
            _fixture.RequestBatch,
            _fixture.Profile.BoundedConcurrencyLimit).GetAwaiter().GetResult();
    }
}
