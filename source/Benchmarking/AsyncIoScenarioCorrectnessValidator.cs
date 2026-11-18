using DotNetPerformanceLab.Data.AsyncIo;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class AsyncIoScenarioCorrectnessValidator : IScenarioCorrectnessValidator
{
    public BenchmarkScenarioId ScenarioId => BenchmarkScenarioId.AsyncIo;

    public ScenarioPreflightResult Validate(DatasetSize datasetSize, JsonBenchmarkGroup jsonGroup)
    {
        AsyncIoBatchFixture fixture = AsyncIoBatchFixtureFactory.CreateAsync(datasetSize).GetAwaiter().GetResult();
        try
        {
            AsyncIoBatchProcessingService service = new();
            AsyncIoBatchCorrectnessVerifier verifier = new(service);
            AsyncIoBatchCorrectnessVerificationResult verification = verifier
                .VerifyAllEquivalentAsync(
                    fixture.HttpClient,
                    fixture.Server,
                    fixture.RequestBatch,
                    fixture.Profile.BoundedConcurrencyLimit)
                .GetAwaiter()
                .GetResult();

            IReadOnlyList<ImplementationPreflightResult> implementations = verification.ResultsByApproach
                .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
                .Select(pair =>
                {
                    AsyncIoBatchProcessingResult result = pair.Value;
                    return new ImplementationPreflightResult(
                        pair.Key.ToString(),
                        Passed: true,
                        $"Completed: {result.CompletedRequestCount:N0}, Max inflight: {result.ServerMaxObservedConcurrentRequests:N0}, Checksum: {result.DeterministicChecksum}");
                })
                .ToArray();

            string whenAllConcurrency = verification.ResultsByApproach[AsyncIoBatchProcessingApproach.TaskWhenAll]
                .ServerMaxObservedConcurrentRequests
                .ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

            return new ScenarioPreflightResult(
                ScenarioId,
                "Async I/O — Retail API batch processing",
                fixture.Profile.RequestCount,
                ScenarioPreflightStatus.Passed,
                $"Validated {implementations.Count} implementations. Sequential max inflight=1, bounded limit={verification.BoundedConcurrencyLimit}, Task.WhenAll observed max inflight={whenAllConcurrency}.",
                implementations);
        }
        finally
        {
            fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
