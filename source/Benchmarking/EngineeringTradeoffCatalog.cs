namespace DotNetPerformanceLab.Benchmarking;

public sealed record EngineeringTradeoffInfo(
    string MethodName,
    string Scenario,
    string HowItWorks,
    string Strengths,
    string CostsAndRisks,
    string WhenToUse,
    string WhenNotToUse,
    string WhatThisBenchmarkMeasures);

public static class EngineeringTradeoffCatalog
{
    private static readonly IReadOnlyDictionary<string, EngineeringTradeoffInfo> EntriesByMethod =
        BuildEntries().ToDictionary(x => x.MethodName, StringComparer.Ordinal);

    public static bool TryGetByMethod(string methodName, out EngineeringTradeoffInfo? info)
    {
        return EntriesByMethod.TryGetValue(methodName, out info);
    }

    public static EngineeringTradeoffInfo GetByMethod(string methodName)
    {
        return EntriesByMethod[methodName];
    }

    public static IReadOnlyList<EngineeringTradeoffInfo> GetByScenario(string scenario)
    {
        return EntriesByMethod.Values
            .Where(x => string.Equals(x.Scenario, scenario, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.MethodName, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<EngineeringTradeoffInfo> GetAll() => EntriesByMethod.Values
        .OrderBy(x => x.Scenario, StringComparer.OrdinalIgnoreCase)
        .ThenBy(x => x.MethodName, StringComparer.Ordinal)
        .ToArray();

    private static IReadOnlyList<EngineeringTradeoffInfo> BuildEntries() =>
    [
        new(
            "EfCoreTracking",
            "Database",
            "Loads full EF entities with tracking enabled, including related customer, order-item, and product data.",
            "Simple update-ready entity graph and straightforward EF usage.",
            "Change tracking increases CPU and managed allocations for read-heavy reporting.",
            "When the same retrieved entities may be updated in the same unit of work.",
            "Pure read-only reporting paths where tracking state is never consumed.",
            "Measures query execution plus entity materialization and tracking overhead; excludes database creation/seeding and correctness preflight."),
        new(
            "EfCoreNoTracking",
            "Database",
            "Loads the same full entity graph but disables EF change tracking via AsNoTracking.",
            "Often lower allocation and less change-tracker CPU for read-only queries.",
            "Returned entities are not tracked, so update workflows require explicit attach/tracking decisions.",
            "Read-only report/query operations where identity tracking is unnecessary.",
            "Workflows that depend on tracked entity state transitions after query.",
            "Measures query execution and full graph materialization without tracking; excludes setup and preflight."),
        new(
            "EfCoreProjection",
            "Database",
            "Projects only required report columns to lean rows and then builds report DTOs.",
            "Reduces object graph size and can lower allocation compared with full entities.",
            "Projection and mapping logic adds query complexity and maintenance overhead.",
            "Stable report shapes where only a subset of entity fields is needed.",
            "Highly dynamic projections where maintainability of custom mapping becomes a burden.",
            "Measures filtered/ordered projection query plus report DTO assembly; excludes schema/seed and preflight."),
        new(
            "EfCoreCompiledProjection",
            "Database",
            "Uses EF compiled query metadata for the same projection shape to reduce repeated translation overhead.",
            "Can reduce recurring query compilation/translation costs in repeated workloads.",
            "Compiled query setup adds specialization complexity and is not guaranteed to help every query pattern.",
            "Hot paths that execute the same projection query shape many times.",
            "Cold or rarely executed queries where compilation reuse is negligible.",
            "Measures compiled projection execution and report assembly; excludes fixture setup and preflight."),
        new(
            "Dapper",
            "Database",
            "Runs parameterized SQL and maps result rows with Dapper before report DTO assembly.",
            "Lower ORM abstraction overhead with simpler mapping than raw data readers.",
            "SQL and mapping are manually owned, increasing schema-coupling and maintenance risk.",
            "Performance-sensitive read paths where manual SQL control is acceptable.",
            "Teams that prioritize EF-level abstraction consistency over hand-authored SQL.",
            "Measures SQL execution, row mapping, and DTO assembly; excludes DB creation/seeding and preflight."),
        new(
            "AdoNet",
            "Database",
            "Uses raw ADO.NET command/reader loops for parameterized SQL and manual row mapping.",
            "Maximum control over command execution and per-row mapping behavior.",
            "Most verbose approach, with higher risk of mapping bugs and lifecycle mistakes.",
            "Narrow hot paths where low-level control and profiling evidence justify complexity.",
            "General business code where readability and maintainability are higher priorities.",
            "Measures SQL execution and manual reader mapping/aggregation; excludes setup and preflight."),

        new(
            "Linq",
            "Collections",
            "Uses LINQ GroupBy and aggregate operators to compute totals and per-category summaries.",
            "Readable declarative code with concise intent expression.",
            "Can allocate grouping/intermediate structures and may add iterator overhead.",
            "Maintainability-first aggregation where measured throughput is sufficient.",
            "Extremely hot loops where profiling shows iterator/allocation overhead is material.",
            "Measures in-memory aggregation and summary construction over deterministic transactions; excludes dataset generation and preflight."),
        new(
            "Foreach",
            "Collections",
            "Uses a single-pass foreach loop with dictionary accumulators for totals and categories.",
            "Good balance of control, readability, and reduced repeated scans.",
            "More imperative state management than LINQ and easier to introduce mutation bugs.",
            "Throughput-sensitive in-memory aggregation with moderate complexity tolerance.",
            "Cases where team preference strongly favors declarative query style and performance is already adequate.",
            "Measures one-pass CPU-bound aggregation and summary construction; excludes fixture generation and preflight."),
        new(
            "PresizedDictionary",
            "Collections",
            "Runs the same single-pass aggregation but pre-sizes dictionary capacity using explicit category-cardinality metadata.",
            "Can reduce dictionary growth/rehash overhead under known cardinality.",
            "Benefit depends on estimate quality; over- or under-estimation can reduce gains.",
            "When cardinality bounds are known and dictionary resizing appears in profiles.",
            "When cardinality is unknown or unstable and extra tuning complexity is not justified.",
            "Measures one-pass aggregation with capacity-aware dictionary initialization; excludes profile/fixture creation and preflight."),
        new(
            "CollectionsMarshal",
            "Collections",
            "Uses CollectionsMarshal.GetValueRefOrAddDefault for by-ref dictionary slot updates inside the aggregation loop.",
            "Can reduce repeated lookup overhead in tight dictionary update paths.",
            "Higher complexity and ref-safety constraints; misuse can cause correctness issues.",
            "Expert-maintained hot paths where profiling confirms dictionary lookup dominates.",
            "General codebases prioritizing simpler, safer dictionary access patterns.",
            "Measures one-pass aggregation with ref-based dictionary updates; excludes dataset setup and preflight."),
        new(
            "ParallelFor",
            "Collections",
            "Uses Parallel.For with thread-local accumulators and a merge phase.",
            "Can scale CPU-bound aggregation on larger datasets and multi-core machines.",
            "Partitioning, synchronization, and merge overhead can outweigh gains for smaller workloads.",
            "Large CPU-bound datasets where parallel overhead is amortized.",
            "Small/medium datasets or constrained-core environments where coordination overhead dominates.",
            "Measures parallel CPU aggregation and merge costs; excludes fixture generation and preflight."),
        new(
            "Plinq",
            "Collections",
            "Uses PLINQ Aggregate with partition-local state and combine functions.",
            "Higher-level parallel abstraction with partitioned local accumulation.",
            "Parallel orchestration and merge cost can reduce or erase benefits on lighter workloads.",
            "Large CPU-bound aggregations where PLINQ partitioning fits workload shape.",
            "Workloads where deterministic single-thread simplicity is more important than potential parallel speedup.",
            "Measures parallel partition processing plus merge behavior; excludes fixture generation and preflight."),

        new(
            "NewtonsoftParserOnly",
            "Json",
            "Deserializes full payload DTOs with Newtonsoft.Json and computes parser-level totals.",
            "Mature serializer features and familiar model-binding behavior.",
            "Reflection and full payload materialization can increase allocations for large payloads.",
            "Compatibility-focused systems that already standardize on Newtonsoft features.",
            "When minimizing dependencies/allocations is critical and required features exist in System.Text.Json.",
            "Measures parse + parser-only aggregate computation; excludes fixture generation and correctness preflight."),
        new(
            "SystemTextJsonParserOnly",
            "Json",
            "Deserializes full payload DTOs with standard System.Text.Json metadata and computes parser-level totals.",
            "Built-in serializer with strong performance defaults and no extra package dependency.",
            "Still materializes full payload object graphs, which can pressure memory on large datasets.",
            "General-purpose .NET services using default serializer behavior.",
            "Payloads requiring unsupported features that force fallback to alternate serializers.",
            "Measures parse + parser-only aggregate computation; excludes setup/preflight."),
        new(
            "SystemTextJsonSourceGenParserOnly",
            "Json",
            "Uses source-generated System.Text.Json metadata for parser-only payload deserialization and totals.",
            "Can reduce runtime reflection/metadata overhead for repeated serialization patterns.",
            "Build-time generation and context maintenance add complexity; gains may be small for tiny payloads.",
            "High-volume serialization paths with stable DTO shapes.",
            "Low-frequency or highly dynamic payloads where extra generation setup adds little value.",
            "Measures parse + parser-only totals with generated metadata; excludes fixture generation and preflight."),
        new(
            "SystemTextJsonStreamingParserOnlyStreamInput",
            "Json",
            "Reads payload incrementally from Stream via Utf8JsonReader state and computes parser totals per order.",
            "Avoids full root payload buffering and demonstrates true incremental stream parsing.",
            "Parsing state management is more complex; final aggregate still depends on all records processed.",
            "Large stream inputs where incremental processing reduces root-object buffering pressure.",
            "Small payloads where simpler typed deserialization is easier to maintain.",
            "Measures stream reads + incremental parse + parser-only totals; excludes fixture generation and preflight."),
        new(
            "NewtonsoftEndToEnd",
            "Json",
            "Deserializes payload with Newtonsoft.Json then transforms all orders into response summaries.",
            "Straightforward end-to-end implementation with mature serializer behavior.",
            "Full object graph and transformed output list allocations can be substantial at scale.",
            "When Newtonsoft compatibility and feature behavior are required end-to-end.",
            "When memory pressure is a dominant concern and simpler alternatives meet requirements.",
            "Measures parse plus response transformation; excludes fixture generation and preflight."),
        new(
            "SystemTextJsonEndToEnd",
            "Json",
            "Deserializes payload with System.Text.Json and performs full response transformation.",
            "Built-in serializer and straightforward end-to-end pipeline.",
            "Still materializes full input graph and output list, so memory grows with payload size.",
            "Common production pipelines with standard System.Text.Json conventions.",
            "When specialized serializer features outside current STJ usage are needed.",
            "Measures parse + transformation output assembly; excludes setup/preflight."),
        new(
            "SystemTextJsonSourceGenEndToEnd",
            "Json",
            "Uses source-generated metadata for end-to-end deserialization and response transformation.",
            "Can reduce metadata/reflection overhead in repeated serialization workloads.",
            "Does not remove core object-materialization costs; complexity may not pay off for all payload sizes.",
            "Stable DTO-heavy high-throughput services with repeated serialization hot paths.",
            "Small or infrequent payload processing where generation maintenance is unnecessary.",
            "Measures parse + transformation with generated metadata; excludes fixture generation and preflight."),
        new(
            "SystemTextJsonStreamingEndToEndStreamInput",
            "Json",
            "Parses incrementally from Stream and transforms each order while reading.",
            "Reduces need for complete root payload buffering while preserving equivalent transformed results.",
            "Output collection still grows with order count; stream parser implementation is more intricate.",
            "Large stream inputs where incremental parsing provides practical memory behavior improvements.",
            "Small payloads where simpler typed deserialization is adequate and easier to debug.",
            "Measures stream reads, incremental parse, and transformation output construction; excludes fixture generation and preflight."),
        new(
            "SequentialAsync",
            "AsyncIo",
            "Issues one HTTP request at a time and awaits each full response before starting the next.",
            "Simplest flow with naturally low pressure on downstream services.",
            "Leaves independent I/O waiting in series, so total batch time tends to scale with request count and per-request latency.",
            "Small batches or strict one-at-a-time integration constraints.",
            "Batches of independent I/O calls where throughput matters.",
            "Measures HTTP request/response, loopback transport, response-body read, JSON deserialization, and deterministic aggregation for serial orchestration."),
        new(
            "TaskWhenAll",
            "AsyncIo",
            "Starts all request tasks for the batch and awaits them together with Task.WhenAll.",
            "Maximizes overlap of independent I/O operations without explicit throttling logic.",
            "Not a throttle; can create high in-flight request counts and burst pressure on server/service capacity.",
            "Bounded, known-size batches where unconstrained fan-out is acceptable.",
            "Downstream systems that require explicit back-pressure or strict concurrency caps.",
            "Measures equivalent HTTP/deserialize/aggregate work under unconstrained task fan-out."),
        new(
            "SemaphoreSlimBounded",
            "AsyncIo",
            "Creates tasks for the whole batch but gates each full awaited request/response operation through SemaphoreSlim.",
            "Applies explicit back-pressure with a clear configurable concurrency limit.",
            "Adds coordination overhead and additional orchestration complexity.",
            "I/O-bound workloads where protecting downstream capacity is important.",
            "Very small batches where throttle overhead outweighs practical benefit.",
            "Measures equivalent HTTP/deserialize/aggregate work with bounded in-flight concurrency."),
        new(
            "ParallelForEachAsyncBounded",
            "AsyncIo",
            "Uses Parallel.ForEachAsync with async request bodies and configured MaxDegreeOfParallelism.",
            "Built-in structured bounded concurrency with concise async loop orchestration.",
            "Parallel-loop scheduling overhead and less direct control than custom semaphore flows in some edge cases.",
            "Async batch processing where bounded parallel request fan-out is desired.",
            "Scenarios requiring fully custom queueing/prioritization semantics beyond loop-level control.",
            "Measures equivalent HTTP/deserialize/aggregate work with async parallel-loop bounded concurrency.")
    ];
}
