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

In interactive mode, the **What are we benchmarking?** preview shows the selected dataset profile values (including category cardinality) and exact compared benchmark method names before final confirmation.

## Implemented algorithms

Basic:

- LINQ `GroupBy` aggregation using declarative grouping/projection.
- Prioritizes readability and maintainability over low-level optimization.

Intermediate:

- Single-pass `foreach` aggregation with dictionary-backed per-category accumulation.
- Balances straightforward imperative code with reduced repeated scans.

Advanced:

- Pre-sized dictionary single-pass aggregation using explicit dataset profile category cardinality.
- `CollectionsMarshal.GetValueRefOrAddDefault` single-pass aggregation to reduce repeated dictionary lookups.
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
- Single-pass dictionary approaches reduce repeated scans and can reduce allocations.
- Pre-sized dictionary variants can reduce dictionary resizing/rehashing when category cardinality is known.
- `CollectionsMarshal` variants can reduce lookup overhead but require careful ref-scoping discipline.
- Parallel approaches add overhead from partitioning, local-state allocation, and merge operations.

For small datasets, parallel overhead may exceed benefits.

## Choosing an approach in production

- Prefer LINQ when clarity and maintainability are primary and measured performance is sufficient.
- Prefer single-pass dictionary aggregation when throughput/allocation pressure is a concern.
- Consider pre-sizing or `CollectionsMarshal` only when profiling shows dictionary hot-path costs are significant and team comfort with complexity is high.
- Consider parallel variants only for workloads large enough to amortize partition/merge overhead on your target hardware.

## Engineering trade-offs

### LINQ

- How it works: declarative GroupBy-based aggregation with projection operators.
- Strengths: high readability and concise intent.
- Costs and risks: iterator/grouping allocations and additional abstraction overhead.
- When to use: maintainability-first flows where measured throughput is acceptable.
- When not to use: extreme hot loops where profiling shows allocation/iterator overhead matters.
- What this benchmark measures: CPU-bound in-memory aggregation and summary construction.

### Foreach single-pass dictionary

- How it works: imperative one-pass aggregation with dictionary accumulators.
- Strengths: balanced control/readability with fewer repeated scans.
- Costs and risks: more mutable state management than declarative LINQ.
- When to use: throughput-sensitive aggregation with moderate complexity tolerance.
- When not to use: codebases that strongly prefer declarative style and already meet perf goals.
- What this benchmark measures: one-pass aggregation and summary assembly.

### Pre-sized dictionary

- How it works: single-pass dictionary aggregation initialized with explicit capacity estimates.
- Strengths: can reduce growth/rehash overhead when cardinality is known.
- Costs and risks: estimate quality drives benefit; poor estimates reduce value.
- When to use: stable known cardinality and profiles showing dictionary resizing cost.
- When not to use: unknown/volatile cardinality where tuning overhead is not justified.
- What this benchmark measures: one-pass aggregation with capacity-aware dictionary initialization.

### CollectionsMarshal ref updates

- How it works: updates dictionary slots via `CollectionsMarshal.GetValueRefOrAddDefault`.
- Strengths: can reduce repeated lookup overhead in tight update paths.
- Costs and risks: higher complexity and ref-safety discipline requirements.
- When to use: expert-maintained hot paths with profiling evidence.
- When not to use: general code where simpler dictionary access is preferable.
- What this benchmark measures: one-pass aggregation with ref-based dictionary updates.

### Parallel.For and PLINQ

- How it works: partitioned parallel processing with thread-/partition-local accumulation and merge.
- Strengths: can improve throughput on large CPU-bound datasets.
- Costs and risks: partitioning/synchronization/merge overhead can dominate smaller workloads.
- When to use: large CPU-bound workloads on multi-core hardware with stable benchmarking conditions.
- When not to use: smaller datasets or constrained-core environments.
- What this benchmark measures: parallel processing plus merge cost under deterministic inputs.

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
