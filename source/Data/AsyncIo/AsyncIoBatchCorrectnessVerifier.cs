namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed class AsyncIoBatchCorrectnessVerifier
{
    private readonly AsyncIoBatchProcessingService _service;

    public AsyncIoBatchCorrectnessVerifier(AsyncIoBatchProcessingService service)
    {
        _service = service;
    }

    public async Task<AsyncIoBatchCorrectnessVerificationResult> VerifyAllEquivalentAsync(
        HttpClient httpClient,
        LocalRetailApiTestServer server,
        IReadOnlyList<AsyncIoRequestKey> requestBatch,
        int boundedConcurrencyLimit,
        CancellationToken cancellationToken = default)
    {
        if (boundedConcurrencyLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(boundedConcurrencyLimit));
        }

        Dictionary<AsyncIoBatchProcessingApproach, AsyncIoBatchProcessingResult> results = new()
        {
            [AsyncIoBatchProcessingApproach.Sequential] = await _service.RunSequentialAsync(httpClient, server, requestBatch, cancellationToken).ConfigureAwait(false),
            [AsyncIoBatchProcessingApproach.TaskWhenAll] = await _service.RunTaskWhenAllAsync(httpClient, server, requestBatch, cancellationToken).ConfigureAwait(false),
            [AsyncIoBatchProcessingApproach.SemaphoreSlimBounded] = await _service.RunSemaphoreBoundedAsync(httpClient, server, requestBatch, boundedConcurrencyLimit, cancellationToken).ConfigureAwait(false),
            [AsyncIoBatchProcessingApproach.ParallelForEachAsync] = await _service.RunParallelForEachAsync(httpClient, server, requestBatch, boundedConcurrencyLimit, cancellationToken).ConfigureAwait(false)
        };

        AsyncIoBatchProcessingResult baseline = results[AsyncIoBatchProcessingApproach.Sequential];

        foreach ((AsyncIoBatchProcessingApproach approach, AsyncIoBatchProcessingResult candidate) in results)
        {
            AssertEquivalent(baseline, candidate, approach, requestBatch.Count);
        }

        AssertSequentialConcurrency(results[AsyncIoBatchProcessingApproach.Sequential]);
        AssertBoundedConcurrency(results[AsyncIoBatchProcessingApproach.SemaphoreSlimBounded], boundedConcurrencyLimit, AsyncIoBatchProcessingApproach.SemaphoreSlimBounded);
        AssertBoundedConcurrency(results[AsyncIoBatchProcessingApproach.ParallelForEachAsync], boundedConcurrencyLimit, AsyncIoBatchProcessingApproach.ParallelForEachAsync);

        return new AsyncIoBatchCorrectnessVerificationResult(results, requestBatch.Count, boundedConcurrencyLimit);
    }

    private static void AssertEquivalent(
        AsyncIoBatchProcessingResult baseline,
        AsyncIoBatchProcessingResult candidate,
        AsyncIoBatchProcessingApproach approach,
        int expectedRequestCount)
    {
        if (candidate.ExpectedRequestCount != expectedRequestCount)
        {
            throw new InvalidOperationException($"Expected request count mismatch for {approach}: expected={expectedRequestCount}, actual={candidate.ExpectedRequestCount}.");
        }

        if (candidate.CompletedRequestCount != expectedRequestCount
            || candidate.SuccessfulResponseCount != expectedRequestCount
            || candidate.ServerTotalRequests != expectedRequestCount)
        {
            throw new InvalidOperationException($"Request completion mismatch for {approach}. Completed={candidate.CompletedRequestCount}, Success={candidate.SuccessfulResponseCount}, ServerTotal={candidate.ServerTotalRequests}, Expected={expectedRequestCount}.");
        }

        if (candidate.ServerActiveRequests != 0)
        {
            throw new InvalidOperationException($"Active request leak detected for {approach}: active={candidate.ServerActiveRequests}.");
        }

        if (candidate.ServerFailedRequests != 0 || candidate.FailedResponseCount != 0)
        {
            throw new InvalidOperationException($"Unexpected failed requests for {approach}: serverFailed={candidate.ServerFailedRequests}, clientFailed={candidate.FailedResponseCount}.");
        }

        if (baseline.CompletedStatusCount != candidate.CompletedStatusCount
            || baseline.TotalAvailableUnits != candidate.TotalAvailableUnits
            || baseline.DeterministicChecksum != candidate.DeterministicChecksum)
        {
            throw new InvalidOperationException($"Result equivalence mismatch for {approach}.");
        }
    }

    private static void AssertSequentialConcurrency(AsyncIoBatchProcessingResult sequential)
    {
        if (sequential.ServerMaxObservedConcurrentRequests > 1)
        {
            throw new InvalidOperationException($"Sequential strategy exceeded expected max concurrency of 1: observed={sequential.ServerMaxObservedConcurrentRequests}.");
        }
    }

    private static void AssertBoundedConcurrency(AsyncIoBatchProcessingResult result, int limit, AsyncIoBatchProcessingApproach approach)
    {
        if (result.ServerMaxObservedConcurrentRequests > limit)
        {
            throw new InvalidOperationException($"{approach} exceeded configured concurrency limit {limit}: observed={result.ServerMaxObservedConcurrentRequests}.");
        }
    }
}
