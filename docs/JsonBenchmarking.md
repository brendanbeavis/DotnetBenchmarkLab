# JSON Benchmarking — Retail API payload processing

## Objective

This scenario compares common .NET JSON approaches for:

1. Deserializing a realistic retail orders API payload.
2. Transforming deserialized orders into an application response summary.

Benchmarks focus on throughput and memory allocation, not parser micro-speed in isolation.

## Payload structure and sizes

Payload includes:

- response metadata (`requestId`, `generatedAtUtc`, `sourceSystem`)
- orders
- nested customer details
- order status/date
- line items with product identifiers/descriptions/quantities/prices/discounts

Deterministic fixture sizes:

- Small: 1,000 orders
- Medium: 50,000 orders
- Large: 250,000 orders

Payload generation uses a fixed seed and stable serialization options. Generation occurs outside measured benchmark methods.

In interactive mode, the **What are we benchmarking?** preview shows dataset-backed payload characteristics (orders, line items, payload bytes), the selected JSON benchmark group, and exact compared benchmark method names before final confirmation.

## Compared approaches

### Newtonsoft.Json

- Typed deserialization using Newtonsoft serializer settings.
- Reflection-based metadata and classic JSON.NET pipeline.

### System.Text.Json standard

- Typed deserialization with regular runtime metadata/reflection behavior.

### System.Text.Json source-generated

- Uses `JsonSerializerContext` generated metadata for scenario DTOs.
- Reduces runtime metadata/reflection overhead.

### System.Text.Json streaming (stream input)

- Consumes a readable `Stream` and incrementally parses UTF-8 with `Utf8JsonReader` + `JsonReaderState` across repeated reads.
- Preserves unconsumed bytes between reads, so tokens split across buffer boundaries are handled correctly.
- Deserializes each order directly from the reader via source-generated metadata (`ApiOrderDto`) without `JsonDocument`, `GetRawText()`, or per-order UTF-8 re-encoding.
- The end-to-end variant still allocates each deserialized order DTO and the final transformed response list.

## Benchmark groups

Two workloads are benchmarked separately:

1. **Parser-only**: parse and compute parser-level aggregate metrics.
2. **End-to-end**: parse plus transformation into response DTO summary.

Within each workload, benchmark types are also separated by input mode:

- **In-memory parse input**: Newtonsoft.Json, System.Text.Json standard, and System.Text.Json source-generated consume the same UTF-8 `byte[]` payload.
- **Stream input**: System.Text.Json streaming consumes a fresh readable stream per invocation.

This separation prevents confusing pure parsing cost with application processing cost.

## Fairness controls

- Same deterministic UTF-8 payload bytes per dataset for all methods.
- No fixture generation in measured methods.
- No reuse of previous deserialized objects.
- Same naming/case/null/number handling conventions.
- Same monetary rounding semantics across approaches.
- Stream-input benchmarks create a fresh read-only in-memory stream each invocation, and measurements include stream reads plus parsing.

## Source generation notes

Source generation improves metadata access and can reduce allocations/startup costs in many scenarios. It does not eliminate all deserialization costs and does not automatically dominate every workload.

## Streaming semantics and trade-offs

Streaming here means incremental processing of orders from a stream reader with an explicit 16 KB read buffer in benchmarks. The parser does not materialize a full root payload DTO. However, end-to-end output still grows with order count because transformed results are collected.

## Engineering trade-offs

### Newtonsoft.Json

- How it works: typed deserialization through JSON.NET runtime metadata/reflection behavior.
- Strengths: mature serializer ecosystem and broad feature compatibility.
- Costs and risks: full payload materialization and reflection-driven metadata can increase allocations.
- When to use: compatibility-focused systems already standardized on Newtonsoft features.
- When not to use: memory-sensitive paths where built-in serializer features are sufficient.
- What this benchmark measures: parser-only parse+aggregate and end-to-end parse+transform cost.

### System.Text.Json standard

- How it works: typed deserialization via built-in serializer runtime metadata.
- Strengths: no external package dependency and strong default performance profile.
- Costs and risks: still materializes complete object graphs for typed payload processing.
- When to use: mainstream .NET services using standard serializer conventions.
- When not to use: payloads requiring features outside the implemented STJ behavior.
- What this benchmark measures: parser-only parse+aggregate and end-to-end parse+transform cost.

### System.Text.Json source-generated

- How it works: uses build-time generated `JsonSerializerContext` metadata.
- Strengths: can reduce runtime metadata/reflection overhead in repeated DTO patterns.
- Costs and risks: extra generation-context maintenance; does not remove core materialization costs.
- When to use: high-volume stable DTO workloads.
- When not to use: low-frequency/small payload workloads where added setup complexity is unnecessary.
- What this benchmark measures: parse and transformation with generated metadata, compared to standard typed paths.

### System.Text.Json streaming (stream input)

- How it works: incremental `Utf8JsonReader` parsing over repeated stream reads with preserved reader state.
- Strengths: avoids full root payload buffering and enables true incremental parse flow.
- Costs and risks: parser-state complexity is higher; end-to-end output allocations still scale with order count.
- When to use: large stream-based inputs where incremental parsing behavior matters.
- When not to use: small payload scenarios where simpler typed deserialization is preferable.
- What this benchmark measures: stream read + incremental parse costs, with parser-only and end-to-end variants.

## Running the JSON scenario

```bash
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group all
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group parser
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group endtoend
```

Dataset can be `small`, `medium`, or `large`.

## Interpreting reports

Use BenchmarkDotNet HTML/Markdown/CSV reports together:

- Mean/variance for execution characteristics
- Allocated bytes for memory pressure
- GC metrics for collection pressure

Hardware/runtime/OS/background workload strongly influence results. Compare runs under consistent conditions.
