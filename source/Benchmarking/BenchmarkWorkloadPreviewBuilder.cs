using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Data.Database;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Data.Json;
using DotNetPerformanceLab.Data.AsyncIo;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class BenchmarkWorkloadPreviewBuilder : IBenchmarkWorkloadPreviewBuilder
{
    public BenchmarkWorkloadPreview Build(BenchmarkRunOptions options)
    {
        IReadOnlyList<BenchmarkScenario> scenarios = options.RunAllScenarios
            ? BenchmarkScenarioRegistry.GetAll().Where(x => x.IsImplemented).ToArray()
            : options.ScenarioId is { } scenarioId
                ? [BenchmarkScenarioRegistry.GetById(scenarioId)]
                : [];

        List<ScenarioWorkloadPreview> scenarioPreviews = new(scenarios.Count);
        foreach (BenchmarkScenario scenario in scenarios)
        {
            ScenarioWorkloadPreview preview = scenario.Id switch
            {
                BenchmarkScenarioId.Database => BuildDatabasePreview(options.DatasetSize),
                BenchmarkScenarioId.Collections => BuildCollectionsPreview(options.DatasetSize),
                BenchmarkScenarioId.Json => BuildJsonPreview(options.DatasetSize, options.JsonGroup),
                BenchmarkScenarioId.AsyncIo => BuildAsyncIoPreview(options.DatasetSize),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario.Id), scenario.Id, "Unsupported scenario.")
            };

            scenarioPreviews.Add(preview);
        }

        return new BenchmarkWorkloadPreview(
            "What are we benchmarking?",
            options.DatasetSize,
            options.RunAllScenarios,
            scenarioPreviews);
    }

    private static ScenarioWorkloadPreview BuildDatabasePreview(DatasetSize datasetSize)
    {
        RetailDatasetProfile profile = RetailDatasetProfiles.Get(datasetSize);
        int estimatedOrderLineCount = RetailDatabaseSeeder.CalculateDeterministicOrderLineCount(profile, BenchmarkConventions.DeterministicSeed);

        DateTime queryStart = profile.BaseOrderDateUtc.AddDays(profile.QueryWindowStartDayOffset);
        DateTime queryEnd = queryStart.AddDays(profile.QueryWindowLengthDays);

        return new ScenarioWorkloadPreview(
            BenchmarkScenarioId.Database,
            "Database — Retail order reporting",
            "Generate a date-windowed retail order report with customer header fields and line-level product/category detail.",
            [
                ("Customers", profile.CustomerCount.ToString("N0")),
                ("Products", profile.ProductCount.ToString("N0")),
                ("Orders", profile.OrderCount.ToString("N0")),
                ("Order lines", estimatedOrderLineCount.ToString("N0")),
                ("Line count per order", $"{profile.MinLinesPerOrder}..{profile.MaxLinesPerOrder}"),
                ("Query window", $"{queryStart:yyyy-MM-dd} to {queryEnd:yyyy-MM-dd} (UTC)")
            ],
            [
                "SQLite local file-backed reads with identical date filtering and ordering.",
                "Two benchmark classes: full-entity materialization and lean projection-based reporting.",
                "Output shape is equivalent report DTOs for all compared methods."
            ],
            [
                BuildMethodPreview("EfCoreTracking"),
                BuildMethodPreview("EfCoreNoTracking"),
                BuildMethodPreview("EfCoreProjection"),
                BuildMethodPreview("EfCoreCompiledProjection"),
                BuildMethodPreview("Dapper"),
                BuildMethodPreview("AdoNet")
            ],
            [
                "Execute one reporting query for the configured date window.",
                "Materialize report DTO output with deterministic ordering and totals."
            ],
            [
                "Database creation and seeding.",
                "Correctness preflight validation.",
                "Interactive menu and process startup overhead."
            ],
            [
                "Entity tracking can increase allocations and CPU for large windows.",
                "Projection paths reduce shape but may shift complexity into mapping.",
                "SQL materialization and mapper overhead vary by abstraction level."
            ],
            [
                ("Dataset", datasetSize.ToString()),
                ("Benchmark groups", "FullEntity + Projection")
            ]);
    }

    private static ScenarioWorkloadPreview BuildCollectionsPreview(DatasetSize datasetSize)
    {
        CollectionDatasetProfile profile = CollectionDatasetProfiles.Get(datasetSize);

        return new ScenarioWorkloadPreview(
            BenchmarkScenarioId.Collections,
            "Collections — Retail transaction aggregation",
            "Aggregate in-memory retail transactions into top-level monetary totals and category-level summaries.",
            [
                ("Transactions", profile.TransactionCount.ToString("N0")),
                ("Products", profile.ProductCount.ToString("N0")),
                ("Category cardinality", profile.CategoryCardinality.ToString("N0")),
                ("Transaction date range", $"{profile.DateRangeDays} days")
            ],
            [
                "CPU-bound in-memory aggregation over deterministic transaction records.",
                "All methods produce equivalent SalesSummary and deterministic category ordering.",
                "Includes sequential and parallel aggregation variants."
            ],
            [
                BuildMethodPreview("Linq"),
                BuildMethodPreview("Foreach"),
                BuildMethodPreview("PresizedDictionary"),
                BuildMethodPreview("CollectionsMarshal"),
                BuildMethodPreview("ParallelFor"),
                BuildMethodPreview("Plinq")
            ],
            [
                "Aggregate every transaction into totals and per-category summaries.",
                "Return full SalesSummary including category transaction counts."
            ],
            [
                "Deterministic fixture generation.",
                "Correctness preflight validation.",
                "Interactive orchestration and process launch overhead."
            ],
            [
                "LINQ can allocate grouping/intermediate structures.",
                "Capacity-aware and ref-based dictionary updates can reduce lookup/resizing overhead.",
                "Parallel partitioning and merge overhead may dominate on smaller datasets."
            ],
            [
                ("Dataset", datasetSize.ToString()),
                ("Benchmark group", "All collection methods")
            ]);
    }

    private static ScenarioWorkloadPreview BuildJsonPreview(DatasetSize datasetSize, JsonBenchmarkGroup jsonGroup)
    {
        JsonDatasetProfile profile = JsonDatasetProfiles.Get(datasetSize);
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(datasetSize);

        IReadOnlyList<BenchmarkMethodPreview> parserOnlyMethods =
        [
            BuildMethodPreview("NewtonsoftParserOnly"),
            BuildMethodPreview("SystemTextJsonParserOnly"),
            BuildMethodPreview("SystemTextJsonSourceGenParserOnly"),
            BuildMethodPreview("SystemTextJsonStreamingParserOnlyStreamInput")
        ];

        IReadOnlyList<BenchmarkMethodPreview> endToEndMethods =
        [
            BuildMethodPreview("NewtonsoftEndToEnd"),
            BuildMethodPreview("SystemTextJsonEndToEnd"),
            BuildMethodPreview("SystemTextJsonSourceGenEndToEnd"),
            BuildMethodPreview("SystemTextJsonStreamingEndToEndStreamInput")
        ];

        IReadOnlyList<BenchmarkMethodPreview> selectedMethods = jsonGroup switch
        {
            JsonBenchmarkGroup.ParserOnly => parserOnlyMethods,
            JsonBenchmarkGroup.EndToEnd => endToEndMethods,
            _ => parserOnlyMethods.Concat(endToEndMethods).ToArray()
        };

        string groupLabel = jsonGroup switch
        {
            JsonBenchmarkGroup.ParserOnly => "ParserOnly",
            JsonBenchmarkGroup.EndToEnd => "EndToEnd",
            _ => "All"
        };

        return new ScenarioWorkloadPreview(
            BenchmarkScenarioId.Json,
            "JSON — Retail API payload processing",
            "Parse deterministic retail orders API payloads and optionally transform orders into response summaries.",
            [
                ("Orders", profile.OrderCount.ToString("N0")),
                ("Customers", profile.CustomerCount.ToString("N0")),
                ("Products", profile.ProductCount.ToString("N0")),
                ("Line items", fixture.TotalLineItemCount.ToString("N0")),
                ("Line items per order", $"{profile.MinLineItemsPerOrder}..{profile.MaxLineItemsPerOrder}"),
                ("Payload UTF-8 bytes", fixture.PayloadUtf8.Length.ToString("N0"))
            ],
            [
                "Compares in-memory typed deserialization paths and dedicated stream-input incremental paths.",
                "Parser-only group computes aggregate metrics; end-to-end group also builds response DTO output.",
                "Same deterministic payload is used across compared methods for the selected group."
            ],
            selectedMethods,
            [
                "Parse payload and compute parser or end-to-end outputs for the selected JSON benchmark group.",
                "Streaming methods include stream reads plus parsing/transformation work."
            ],
            [
                "Payload fixture generation.",
                "Correctness preflight validation.",
                "Interactive orchestration and process startup overhead."
            ],
            [
                "Typed object materialization affects allocation pressure.",
                "Source generation can reduce metadata overhead in many workloads.",
                "Stream-input variants include read-buffer behavior and may trade simplicity for memory behavior."
            ],
            [
                ("Dataset", datasetSize.ToString()),
                ("JSON benchmark group", groupLabel)
            ]);
    }

    private static BenchmarkMethodPreview BuildMethodPreview(string methodName)
    {
        EngineeringTradeoffInfo info = EngineeringTradeoffCatalog.GetByMethod(methodName);
        return new BenchmarkMethodPreview(
            methodName,
            info.HowItWorks,
            $"Use: {info.WhenToUse} Risk: {info.CostsAndRisks}");
    }

    private static ScenarioWorkloadPreview BuildAsyncIoPreview(DatasetSize datasetSize)
    {
        AsyncIoDatasetProfile profile = AsyncIoDatasetProfiles.Get(datasetSize);

        return new ScenarioWorkloadPreview(
            BenchmarkScenarioId.AsyncIo,
            "Async I/O — Retail API batch processing",
            "Fetch deterministic order-status/availability records for a batch of independent request keys using local loopback HTTP.",
            [
                ("Requests", profile.RequestCount.ToString("N0")),
                ("Store cardinality", profile.StoreCount.ToString("N0")),
                ("Simulated server latency", $"{profile.SimulatedLatencyMilliseconds} ms/request"),
                ("Bounded concurrency limit", profile.BoundedConcurrencyLimit.ToString("N0"))
            ],
            [
                "In-process local Kestrel server bound to loopback with deterministic JSON responses.",
                "All methods issue identical endpoints, deserialize the same DTO, and aggregate the same deterministic checksum.",
                "Sequential, unbounded fan-out, and bounded concurrency orchestration strategies are compared."
            ],
            [
                BuildMethodPreview("SequentialAsync"),
                BuildMethodPreview("TaskWhenAll"),
                BuildMethodPreview("SemaphoreSlimBounded"),
                BuildMethodPreview("ParallelForEachAsyncBounded")
            ],
            [
                "HTTP request creation, loopback transport, response body read, JSON deserialization, and deterministic aggregation.",
                "Server-side latency simulation and concurrent request processing behavior."
            ],
            [
                "Server startup/shutdown and HttpClient construction.",
                "Request-batch generation and correctness preflight.",
                "Interactive orchestration and process launch overhead."
            ],
            [
                "Task.WhenAll starts all requests and is not a throttle.",
                "SemaphoreSlim and Parallel.ForEachAsync apply explicit bounded concurrency back-pressure.",
                "Observed concurrency can be constrained by local host/server capacity."
            ],
            [
                ("Dataset", datasetSize.ToString()),
                ("Benchmark group", "BatchHttp orchestration")
            ]);
    }
}
