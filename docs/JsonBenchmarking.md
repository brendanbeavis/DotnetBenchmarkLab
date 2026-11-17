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

## Compared approaches

### Newtonsoft.Json

- Typed deserialization using Newtonsoft serializer settings.
- Reflection-based metadata and classic JSON.NET pipeline.

### System.Text.Json standard

- Typed deserialization with regular runtime metadata/reflection behavior.

### System.Text.Json source-generated

- Uses `JsonSerializerContext` generated metadata for scenario DTOs.
- Reduces runtime metadata/reflection overhead.

### System.Text.Json streaming

- Uses `Utf8JsonReader` to incrementally locate/read the `orders` array.
- Deserializes one order object at a time and transforms immediately.
- Avoids retaining the full payload object graph in memory for processing.

## Benchmark groups

Two groups are benchmarked separately:

1. **Parser-only**: parse and compute parser-level aggregate metrics.
2. **End-to-end**: parse plus transformation into response DTO summary.

This separation prevents confusing pure parsing cost with application processing cost.

## Fairness controls

- Same UTF-8 payload bytes per dataset for all methods.
- No fixture generation in measured methods.
- No reuse of previous deserialized objects.
- Same naming/case/null/number handling conventions.
- Same monetary rounding semantics across approaches.

## Source generation notes

Source generation improves metadata access and can reduce allocations/startup costs in many scenarios. It does not eliminate all deserialization costs and does not automatically dominate every workload.

## Streaming semantics and trade-offs

Streaming here means incremental processing of orders from a reader, retaining transformed outputs but not a complete root payload graph. This can reduce memory pressure for large payloads, but may add implementation complexity.

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
