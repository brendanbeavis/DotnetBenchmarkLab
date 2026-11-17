# Benchmarking Best Practices

## Build and execution mode

- Use **Release** configuration.
- Run benchmarks **outside the debugger**.
- Prefer isolated benchmark execution (BenchmarkDotNet default process model).

## Warm-up and measurement

- Let BenchmarkDotNet control warm-up and measurement iterations.
- Avoid manual stopwatch loops for comparative claims.
- Use multiple runs before concluding behavior differences.

## Environment consistency

- Keep hardware and CPU topology consistent.
- Use stable power settings.
- Minimize background CPU/memory/disk/network load.
- Record runtime/SDK/OS information when sharing results.

## Dataset realism

- Use deterministic seeds.
- Choose representative data sizes for the real-world problem.
- Keep fixture generation outside measured benchmark methods.

## Methodology correctness

- Compare equivalent logical work.
- Keep serializer/database/query options aligned across compared methods.
- Avoid mutating shared input data inside benchmark methods.
- Return computed results (or consume them) to reduce dead-code elimination risk.

## Memory and GC interpretation

- Time alone is not enough.
- Review allocated bytes and GC generation counts.
- Lower allocation often improves consistency under sustained throughput.

## Reporting and sharing

- Preserve run artifacts (HTML/Markdown/CSV) with timestamps or snapshots.
- Include environment details and benchmark parameters with shared reports.
- Avoid claiming universal winners; results are context-dependent.

## Adding a fourth scenario

1. Define a real-world engineering problem.
2. Create deterministic input data with fixed-seed generation.
3. Implement multiple equivalent approaches.
4. Add correctness/equivalence tests before benchmarking claims.
5. Add BenchmarkDotNet benchmark classes with sensible baselines.
6. Register the scenario in the CLI/menu/runner mapping.
7. Document trade-offs and interpretation boundaries.
8. Verify report exporters and artifact discovery in the terminal UI.
