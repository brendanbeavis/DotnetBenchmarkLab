# Architecture note: interactive UI vs benchmark execution

## Goal

Keep developer interaction and benchmark measurement concerns separated while sharing scenario registration and run configuration.

## Separation model

- **Interactive mode** (`dotnet run`) hosts Spectre.Console menus for scenario selection, dataset choice, group choice (for JSON), a pre-run "What are we benchmarking?" preview, correctness preflight, and report discovery.
- **Benchmark mode** (`--benchmark ...`) is a dedicated execution path that delegates timing/statistics to BenchmarkDotNet.

A shared CLI parser and run-options model drives both entry points.

## Why this matters

BenchmarkDotNet owns warm-up, iteration control, process isolation, statistics, and exporters. The app does not use ad-hoc Stopwatch loops for authoritative performance claims.

## Isolation approach

Interactive launches run benchmarks in a child process (`dotnet <assembly> --benchmark ...`) so menu output is not interleaved with benchmark output.

The workload preview and correctness preflight run in the interactive parent process before child-process launch. These steps are intentionally excluded from BenchmarkDotNet measured operations.

After benchmark completion, the benchmark process renders a supplementary **Performance Lab Summary** derived from structured BenchmarkDotNet result objects. This summary is intended for quick interpretation and does not replace native BenchmarkDotNet reports/exporters.

## Current implemented scenarios

- Database (SQLite retail order reporting)
- Collections (retail transaction aggregation)
- JSON (payload deserialization and transformation)

All scenarios use deterministic data fixtures and correctness tests to validate equivalence across compared implementations.
