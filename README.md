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

Each scenario includes Basic, Intermediate, and Advanced approaches with correctness-equivalence tests.

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

## Run benchmarks directly (CLI mode)

```bash
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark all --dataset medium

dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark database --dataset small

dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark collections --dataset small

dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group all
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group parser
dotnet run -c Release --project BenchmarkLab/BenchmarkLab.csproj -- --benchmark json --dataset small --json-group endtoend
```

## Report output

BenchmarkDotNet exporters generate:

- HTML report
- Markdown report
- CSV report

Reports are discovered from `BenchmarkDotNet.Artifacts/results` candidate locations (current working directory and app-base fallback), and listed in the terminal UI sorted by modification time.

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
