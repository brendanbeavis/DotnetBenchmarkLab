# DotNetPerformanceLab

DotNetPerformanceLab is an internal developer education project for practical .NET performance benchmarking.

It compares alternative implementations of realistic .NET workloads across:

- execution time
- throughput
- memory allocation
- garbage collection behavior

Authoritative measurements are produced by BenchmarkDotNet.

## Implemented scenarios

- **Database — Retail order reporting (SQLite)**
- **Collections — Retail transaction aggregation**
- **JSON — Retail API payload processing**
- **Async I/O — Retail API test batch processing**

Each scenario includes Basic, Intermediate, and Advanced approaches with correctness-equivalence tests.

The Async I/O scenario tests HTTP batch orchestration against a deterministic local loopback API server. It compares sequential async requests, `Task.WhenAll`, `SemaphoreSlim`-bounded concurrency, and `Parallel.ForEachAsync`-bounded concurrency. The server simulates configurable latency and returns deterministic order-status/availability responses, while each approach performs equivalent response deserialization and result aggregation.

For the JSON scenario, in-memory typed deserialization benchmarks and stream-input incremental parsing benchmarks are reported as separate benchmark types.

For the collections scenario, Basic/Intermediate/Advanced compare distinct strategies: LINQ GroupBy readability, single-pass dictionary aggregation, capacity-aware/CollectionsMarshal dictionary tuning, and parallel CPU aggregation.

## Prerequisites

- .NET 10 SDK
- Windows, Linux, or macOS terminal

## Restore

```bash
dotnet restore BenchmarkLab.slnx
```

## Build (Release)

```bash
dotnet build BenchmarkLab.slnx -c Release
```

## Test (Release)

```bash
dotnet test BenchmarkLab.slnx -c Release
```

## Run interactive app

```bash
dotnet run --project BenchmarkLab/BenchmarkLab.csproj
```

Interactive mode shows a **What are we benchmarking?** preview before final run confirmation. It lists business problem, deterministic dataset characteristics, exact benchmark method names, measured operation scope, and excluded setup work.

## Run benchmarks directly (CLI mode)

```bash
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark all --dataset medium

dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark database --dataset small

dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark collections --dataset small

dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group all
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group parser
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group endtoend

dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark asyncio --dataset small
```

## Report output

BenchmarkDotNet exporters generate:

- HTML report
- Markdown report
- CSV report

The app also prints a supplementary **Performance Lab Summary** after each run to provide a quick, beginner-friendly comparison of time and managed allocation metrics. BenchmarkDotNet native reports remain the authoritative detailed source.

Reports are discovered from `BenchmarkDotNet.Artifacts/results` candidate locations (current working directory and app-base fallback), and listed in the terminal UI sorted by modification time.

For the Database (SQLite) scenario, benchmark fixtures create unique temporary database files under the OS temp directory and remove owned `.db`, `-wal`, and `-shm` files during fixture cleanup after correctness checks and benchmark runs.

## Documentation

- `docs/DatabaseBenchmarking.md`
- `docs/CollectionBenchmarking.md`
- `docs/JsonBenchmarking.md`
- `docs/BenchmarkingBestPractices.md`
- `docs/architecture/interactive-vs-benchmark-execution.md`

## Important measurement warning

Benchmark results depend on environment details, including:

- hardware (CPU/cache/memory)
- runtime/JIT behavior
- operating system
- power plan and thermal state
- background workload

Use consistent machines and settings when comparing runs. Avoid drawing production-wide conclusions from a single microbenchmark run.
