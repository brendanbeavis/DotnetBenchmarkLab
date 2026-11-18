namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkScenarioRegistry
{
    private static readonly IReadOnlyList<BenchmarkScenario> Scenarios =
    [
        new(
            BenchmarkScenarioId.Database,
            "Database — Retail order reporting",
            "Compare EF Core tracking/no-tracking/projection/compiled-query against Dapper and raw ADO.NET over SQLite.",
            [
                "Understand ORM tracking overhead trade-offs.",
                "Compare abstraction cost against lower-level data access.",
                "Learn how query shape impacts allocations and throughput."
            ],
            IsImplemented: true,
            "Implements full-entity and lean-projection comparisons over deterministic local SQLite data."),
        new(
            BenchmarkScenarioId.Collections,
            "Collections — Retail transaction aggregation",
            "Compare LINQ, foreach, Dictionary aggregation, Parallel.For and PLINQ on deterministic in-memory transactions.",
            [
                "Measure iterator overhead and allocation differences.",
                "Compare single-threaded and parallel aggregation patterns.",
                "Identify when parallelism helps or hurts."
            ],
            IsImplemented: true,
            "Implements equivalent aggregation comparisons over deterministic retail transaction datasets."),
        new(
            BenchmarkScenarioId.Json,
            "JSON — Retail API payload processing",
            "Compare Newtonsoft.Json, System.Text.Json, source-generated System.Text.Json and streaming deserialization.",
            [
                "Understand serializer feature/performance trade-offs.",
                "Compare allocation profiles for DOM vs streaming paths.",
                "Learn safe defaults for high-volume payload processing."
            ],
            IsImplemented: true,
            "Implements parser-only and end-to-end JSON processing comparisons over deterministic payload fixtures."),
        new(
            BenchmarkScenarioId.AsyncIo,
            "Async I/O — Retail API batch processing",
            "Compare sequential async, Task.WhenAll, SemaphoreSlim-bounded concurrency, and Parallel.ForEachAsync over deterministic local HTTP requests.",
            [
                "Understand I/O-bound async overlap versus CPU-bound parallelism.",
                "Learn why Task.WhenAll is not a throttle.",
                "Measure bounded concurrency back-pressure trade-offs."
            ],
            IsImplemented: true,
            "Implements local loopback HTTP batch processing with deterministic responses, latency simulation, and concurrency instrumentation.")
    ];

    public static IReadOnlyList<BenchmarkScenario> GetAll() => Scenarios;

    public static BenchmarkScenario GetById(BenchmarkScenarioId id) =>
        Scenarios.First(s => s.Id == id);
}
