using BenchmarkDotNet.Attributes;
using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Database;
using DotNetPerformanceLab.Reporting.Database;

namespace DotNetPerformanceLab.Benchmarks.Database;

/// <summary>
/// Full-entity materialization comparisons where EF tracking overhead is intentionally visible.
/// </summary>
[MemoryDiagnoser]
public class DatabaseReportingBenchmarks
{
    [ParamsSource(nameof(DatasetSizes))]
    public string Dataset { get; set; } = nameof(DatasetSize.Medium);

    public IEnumerable<string> DatasetSizes => [BenchmarkConventions.ToParameterValue(BenchmarkRuntimeContext.DatasetSize)];

    private DatabaseReportingQueryService _queryService = null!;
    private DatabaseReportQuery _query = null!;

    [GlobalSetup]
    public void Setup()
    {
        DatasetSize datasetSize = Enum.Parse<DatasetSize>(Dataset, ignoreCase: true);
        RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(datasetSize);
        _queryService = new DatabaseReportingQueryService(fixture);
        _query = fixture.ReportQuery;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Database", "Basic", "FullEntity")]
    public IReadOnlyList<DatabaseOrderReportDto> EfCoreTracking()
    {
        return _queryService.RunEfTracking(_query);
    }

    [Benchmark]
    [BenchmarkCategory("Database", "Intermediate", "FullEntity")]
    public IReadOnlyList<DatabaseOrderReportDto> EfCoreNoTracking()
    {
        return _queryService.RunEfNoTracking(_query);
    }
}

/// <summary>
/// Lean projection comparisons where each approach selects only required report fields.
/// </summary>
[MemoryDiagnoser]
public class DatabaseReportingProjectionBenchmarks
{
    [ParamsSource(nameof(DatasetSizes))]
    public string Dataset { get; set; } = nameof(DatasetSize.Medium);

    public IEnumerable<string> DatasetSizes => [BenchmarkConventions.ToParameterValue(BenchmarkRuntimeContext.DatasetSize)];

    private DatabaseReportingQueryService _queryService = null!;
    private DatabaseReportQuery _query = null!;

    [GlobalSetup]
    public void Setup()
    {
        DatasetSize datasetSize = Enum.Parse<DatasetSize>(Dataset, ignoreCase: true);
        RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(datasetSize);
        _queryService = new DatabaseReportingQueryService(fixture);
        _query = fixture.ReportQuery;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Database", "Intermediate", "Projection")]
    public IReadOnlyList<DatabaseOrderReportDto> EfCoreProjection()
    {
        return _queryService.RunEfProjection(_query);
    }

    [Benchmark]
    [BenchmarkCategory("Database", "Advanced", "Projection")]
    public IReadOnlyList<DatabaseOrderReportDto> EfCoreCompiledProjection()
    {
        return _queryService.RunEfCompiledProjection(_query);
    }

    [Benchmark]
    [BenchmarkCategory("Database", "Advanced", "Projection")]
    public IReadOnlyList<DatabaseOrderReportDto> Dapper()
    {
        return _queryService.RunDapper(_query);
    }

    [Benchmark]
    [BenchmarkCategory("Database", "Advanced", "Projection")]
    public IReadOnlyList<DatabaseOrderReportDto> AdoNet()
    {
        return _queryService.RunAdoNet(_query);
    }
}
