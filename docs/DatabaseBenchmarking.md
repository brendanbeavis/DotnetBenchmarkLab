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

## SQLite-specific notes and limitations

- SQLite decimal handling differs from SQL Server; this scenario uses consistent decimal conversion and C# rounding for report totals.
- WAL and related pragmas are applied consistently in fixture setup.
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
