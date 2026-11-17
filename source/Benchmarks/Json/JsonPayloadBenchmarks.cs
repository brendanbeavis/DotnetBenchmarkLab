using BenchmarkDotNet.Attributes;
using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Data.Json;
using DotNetPerformanceLab.Reporting.Json;

namespace DotNetPerformanceLab.Benchmarks.Json;

[MemoryDiagnoser]
public class JsonPayloadParsingBenchmarks
{
    [ParamsSource(nameof(DatasetSizes))]
    public string Dataset { get; set; } = nameof(DatasetSize.Medium);

    public IEnumerable<string> DatasetSizes => [BenchmarkConventions.ToParameterValue(BenchmarkRuntimeContext.DatasetSize)];

    private JsonPayloadProcessingService _service = null!;
    private byte[] _payloadUtf8 = [];

    [GlobalSetup]
    public void Setup()
    {
        DatasetSize datasetSize = Enum.Parse<DatasetSize>(Dataset, ignoreCase: true);
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(datasetSize);
        _payloadUtf8 = fixture.PayloadUtf8;
        _service = new JsonPayloadProcessingService();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Json", "Basic", "ParserOnly")]
    public JsonParserOnlyResultDto NewtonsoftParserOnly()
    {
        return _service.ParseOnlyNewtonsoft(_payloadUtf8);
    }

    [Benchmark]
    [BenchmarkCategory("Json", "Intermediate", "ParserOnly")]
    public JsonParserOnlyResultDto SystemTextJsonParserOnly()
    {
        return _service.ParseOnlySystemTextJson(_payloadUtf8);
    }

    [Benchmark]
    [BenchmarkCategory("Json", "Advanced", "ParserOnly")]
    public JsonParserOnlyResultDto SystemTextJsonSourceGenParserOnly()
    {
        return _service.ParseOnlySystemTextJsonSourceGenerated(_payloadUtf8);
    }

    [Benchmark]
    [BenchmarkCategory("Json", "Advanced", "ParserOnly")]
    public JsonParserOnlyResultDto SystemTextJsonStreamingParserOnly()
    {
        return _service.ParseOnlySystemTextJsonStreaming(_payloadUtf8);
    }
}

[MemoryDiagnoser]
public class JsonPayloadEndToEndBenchmarks
{
    [ParamsSource(nameof(DatasetSizes))]
    public string Dataset { get; set; } = nameof(DatasetSize.Medium);

    public IEnumerable<string> DatasetSizes => [BenchmarkConventions.ToParameterValue(BenchmarkRuntimeContext.DatasetSize)];

    private JsonPayloadProcessingService _service = null!;
    private byte[] _payloadUtf8 = [];

    [GlobalSetup]
    public void Setup()
    {
        DatasetSize datasetSize = Enum.Parse<DatasetSize>(Dataset, ignoreCase: true);
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(datasetSize);
        _payloadUtf8 = fixture.PayloadUtf8;
        _service = new JsonPayloadProcessingService();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Json", "Basic", "EndToEnd")]
    public JsonProcessingResultDto NewtonsoftEndToEnd()
    {
        return _service.ProcessNewtonsoft(_payloadUtf8);
    }

    [Benchmark]
    [BenchmarkCategory("Json", "Intermediate", "EndToEnd")]
    public JsonProcessingResultDto SystemTextJsonEndToEnd()
    {
        return _service.ProcessSystemTextJson(_payloadUtf8);
    }

    [Benchmark]
    [BenchmarkCategory("Json", "Advanced", "EndToEnd")]
    public JsonProcessingResultDto SystemTextJsonSourceGenEndToEnd()
    {
        return _service.ProcessSystemTextJsonSourceGenerated(_payloadUtf8);
    }

    [Benchmark]
    [BenchmarkCategory("Json", "Advanced", "EndToEnd")]
    public JsonProcessingResultDto SystemTextJsonStreamingEndToEnd()
    {
        return _service.ProcessSystemTextJsonStreaming(_payloadUtf8);
    }
}
