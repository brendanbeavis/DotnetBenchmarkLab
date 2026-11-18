# Database Benchmarking — Retail order reporting (SQLite)

## Problem

This scenario benchmarks realistic date-filtered retail reporting queries that must return:

- order header fields
- customer identity/region fields
- order line item details
- product/category fields
- calculated line totals and order totals

All implementations run against the same local SQLite data and return the same logical DTO output.

## Schema and generated data

Entities:

- Customer: `Id`, `Name`, `Email`, `Region`
- Product: `Id`, `Sku`, `Name`, `Category`, `UnitPrice`
- Order: `Id`, `CustomerId`, `OrderDate`, `Status`
- OrderItem: `Id`, `OrderId`, `ProductId`, `Quantity`, `UnitPrice`, `DiscountPercent`

Configuration includes foreign keys and query-relevant indexes (`OrderDate`, customer/order joins, order-item joins, product/category lookups).

Data is seeded deterministically with fixed seed `424242`.

Dataset targets:

- Small: ~1,000 orders
- Medium: ~50,000 orders
- Large: ~250,000 orders

Database creation and seeding run in setup only (outside measured benchmark methods).

## Implementations and learning levels

Basic:

- EF Core tracking (full entity materialization)

Intermediate:

- EF Core `AsNoTracking` (same entity graph without change tracker)
- EF Core DTO projection (select only fields needed for report)

Advanced:

- EF Core compiled projection query
- Dapper parameterized SQL projection
- Raw ADO.NET parameterized SQL reader mapping

## Measurement boundaries

In interactive mode, a pre-run **What are we benchmarking?** screen shows exact dataset values, compared method names, and measurement boundaries before confirmation.

Included in measured methods:

- query execution
- result materialization
- report DTO assembly and aggregate calculations
- per-invocation connection/context acquisition for each approach

Excluded from measured methods:

- schema creation
- seeding
- fixture initialization

BenchmarkDotNet handles warmup/iteration/process isolation.

## Comparison boundaries

Two benchmark groups are used to avoid misleading comparisons:

1. **Full-entity group**: EF tracking vs EF no-tracking
2. **Lean-projection group**: EF projection vs compiled projection vs Dapper vs ADO.NET

Tracking naturally materializes more state; projection-focused methods intentionally fetch only report fields.

## EF compiled query caveats

Compiled queries can reduce repeated translation overhead, but gains depend on:

- query shape complexity
- execution frequency
- cache/JIT effects

It is not a universal win for all workloads.

## Dapper and ADO.NET trade-offs

- Dapper: lower abstraction than EF with less mapping boilerplate than raw readers
- ADO.NET: maximum control, highest mapping verbosity

Both use parameterized SQL to avoid SQL injection and preserve plan stability.

## Engineering trade-offs

### EF Core tracking

- How it works: materializes full entity graphs with change tracking enabled.
- Strengths: straightforward EF usage and update-ready entities.
- Costs and risks: higher CPU/allocation overhead for read-only reporting.
- When to use: read + update workflows in the same unit of work.
- When not to use: pure read paths with no tracked-state usage.
- What this benchmark measures: query execution plus full graph materialization and tracking overhead.

### EF Core no-tracking

- How it works: materializes the same entity graph with tracking disabled.
- Strengths: reduced tracking overhead for read-focused workloads.
- Costs and risks: entities are detached, so updates require explicit attach/tracking decisions.
- When to use: read-only query/reporting endpoints.
- When not to use: workflows that depend on implicit tracked state transitions.
- What this benchmark measures: query + full graph materialization without tracker bookkeeping.

### EF Core projection and compiled projection

- How it works: projects only report fields; compiled variant reuses query compilation metadata.
- Strengths: leaner shape and potential translation-overhead reduction for repeated query shapes.
- Costs and risks: mapping/query specialization complexity; gains vary by query pattern.
- When to use: stable report DTO shapes and high-frequency repeated query execution.
- When not to use: low-frequency or highly dynamic queries where specialization adds maintenance cost.
- What this benchmark measures: filtered/ordered projection execution and report DTO assembly.

### Dapper

- How it works: executes parameterized SQL and maps rows via Dapper.
- Strengths: lower abstraction overhead than full ORM graph materialization.
- Costs and risks: SQL and mapping ownership increase schema-coupling and maintenance burden.
- When to use: performance-sensitive read paths where manual SQL control is acceptable.
- When not to use: teams favoring EF-level abstraction consistency for most data access.
- What this benchmark measures: SQL execution + row mapping + DTO assembly.

### ADO.NET

- How it works: executes parameterized SQL and maps rows manually with data readers.
- Strengths: maximal control over SQL and per-row mapping behavior.
- Costs and risks: highest verbosity and highest mapping-maintenance risk.
- When to use: narrow hot paths where profiling supports low-level control.
- When not to use: general business queries where readability and maintainability dominate.
- What this benchmark measures: SQL execution and manual reader mapping/aggregation.

## SQLite-specific notes and limitations

- SQLite decimal handling differs from SQL Server; this scenario uses consistent decimal conversion and C# rounding for report totals.
- WAL and related pragmas are applied consistently in fixture setup.
- The scenario fixture owns a unique temporary SQLite path under the OS temp directory and performs deterministic cleanup in fixture disposal (`.db`, `-wal`, `-shm`).
- Connection pooling remains enabled for benchmark parity; fixture cleanup explicitly clears pooled SQLite connections before deleting files.
- Results here are useful for approach education, but they are **not** a substitute for production SQL Server benchmarking.

## Interpreting metrics

Use BenchmarkDotNet outputs together:

- **Time**: mean/median/error distributions
- **Allocation**: bytes allocated per operation
- **GC**: Gen0/Gen1/Gen2 pressure

Focus on reproducible trends, not single-run absolute values.

## Run only this scenario

```bash
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark database --dataset small
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark database --dataset medium
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark database --dataset large
```

Reports are written under BenchmarkDotNet artifacts (HTML/Markdown/CSV exporters enabled).
