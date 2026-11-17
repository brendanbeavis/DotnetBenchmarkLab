# Collection Benchmarking — Retail transaction aggregation

## Problem

This scenario benchmarks CPU-intensive in-memory processing of retail transactions into a `SalesSummary` containing:

- total transaction count
- total units sold
- gross sales
- total discounts
- net sales
- category-level sales aggregates
- category-level transaction counts

All implementations process identical input data and produce equivalent output semantics.

## Dataset shape and sizes

Transactions include:

- `TransactionId`
- `ProductId`
- `Category`
- `Quantity`
- `UnitPrice`
- `DiscountPercent`
- `TransactionDate`

Generation is deterministic with fixed seed `424242`.

Sizes:

- Small: 10,000 transactions
- Medium: 250,000 transactions
- Large: 1,000,000 transactions

Data generation occurs in setup only, never in measured benchmark methods.

## Implemented algorithms

Basic:

- LINQ aggregation using grouping and projection.

Intermediate:

- `foreach` imperative aggregation.
- Dictionary-based aggregation with single-pass category accumulation.

Advanced:

- `Parallel.For` with thread-local accumulators and post-merge reduction.
- PLINQ `Aggregate` with partition-local aggregation and merge.

## Monetary semantics

Financial values use `decimal` throughout.

Consistent rounding:

- 2 decimal places
- `MidpointRounding.AwayFromZero`

All approaches apply the same discount and net calculation rules.

## Allocation and execution considerations

- LINQ can introduce intermediate enumerables/grouping allocations.
- Dictionary and loop-based approaches reduce repeated scans and can reduce allocations.
- Parallel approaches add overhead from partitioning, local-state allocation, and merge operations.

For small datasets, parallel overhead may exceed benefits.

## Parallel characteristics and caveats

Parallel implementations avoid shared mutable writes per transaction by:

- using thread-local/per-partition accumulators
- merging once per local partition

This avoids a single global lock on every transaction.

## Hardware impact

Parallel results are highly environment-dependent:

- CPU core count
- memory bandwidth/cache behavior
- thread scheduling
- background system load

Do not assume one method is universally fastest.

## Running only this scenario

```bash
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark collections --dataset small
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark collections --dataset medium
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark collections --dataset large
```

BenchmarkDotNet reports are generated as HTML, Markdown, and CSV artifacts.
