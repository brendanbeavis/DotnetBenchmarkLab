using System.Collections.Concurrent;
using System.Text.Json;

namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed class AsyncIoBatchProcessingService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AsyncIoBatchProcessingResult> RunSequentialAsync(
        HttpClient httpClient,
        LocalRetailApiTestServer server,
        IReadOnlyList<AsyncIoRequestKey> requestBatch,
        CancellationToken cancellationToken = default)
    {
        server.ResetMetrics();

        List<AsyncIoApiResponseDto> responses = new(requestBatch.Count);
        foreach (AsyncIoRequestKey request in requestBatch)
        {
            responses.Add(await SendAndDeserializeAsync(httpClient, request, cancellationToken).ConfigureAwait(false));
        }

        return BuildResult(requestBatch.Count, responses, server.GetSnapshot());
    }

    public async Task<AsyncIoBatchProcessingResult> RunTaskWhenAllAsync(
        HttpClient httpClient,
        LocalRetailApiTestServer server,
        IReadOnlyList<AsyncIoRequestKey> requestBatch,
        CancellationToken cancellationToken = default)
    {
        server.ResetMetrics();

        Task<AsyncIoApiResponseDto>[] tasks = requestBatch
            .Select(request => SendAndDeserializeAsync(httpClient, request, cancellationToken))
            .ToArray();

        AsyncIoApiResponseDto[] responses = await Task.WhenAll(tasks).ConfigureAwait(false);
        return BuildResult(requestBatch.Count, responses, server.GetSnapshot());
    }

    public async Task<AsyncIoBatchProcessingResult> RunSemaphoreBoundedAsync(
        HttpClient httpClient,
        LocalRetailApiTestServer server,
        IReadOnlyList<AsyncIoRequestKey> requestBatch,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        }

        server.ResetMetrics();

        ConcurrentBag<AsyncIoApiResponseDto> responses = [];
        using SemaphoreSlim gate = new(maxConcurrency, maxConcurrency);

        Task[] tasks = requestBatch.Select(async request =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                AsyncIoApiResponseDto response = await SendAndDeserializeAsync(httpClient, request, cancellationToken).ConfigureAwait(false);
                responses.Add(response);
            }
            finally
            {
                gate.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return BuildResult(requestBatch.Count, responses, server.GetSnapshot());
    }

    public async Task<AsyncIoBatchProcessingResult> RunParallelForEachAsync(
        HttpClient httpClient,
        LocalRetailApiTestServer server,
        IReadOnlyList<AsyncIoRequestKey> requestBatch,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
        }

        server.ResetMetrics();

        ConcurrentBag<AsyncIoApiResponseDto> responses = [];
        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = maxConcurrency,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(requestBatch, options, async (request, ct) =>
        {
            AsyncIoApiResponseDto response = await SendAndDeserializeAsync(httpClient, request, ct).ConfigureAwait(false);
            responses.Add(response);
        }).ConfigureAwait(false);

        return BuildResult(requestBatch.Count, responses, server.GetSnapshot());
    }

    private async Task<AsyncIoApiResponseDto> SendAndDeserializeAsync(HttpClient httpClient, AsyncIoRequestKey request, CancellationToken cancellationToken)
    {
        string url = $"/retail/orders/{request.StoreId}/{request.OrderId}?requestId={Uri.EscapeDataString(request.RequestId)}";

        using HttpRequestMessage message = new(HttpMethod.Get, url);
        using HttpResponseMessage response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream bodyStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        AsyncIoApiResponseDto? payload = await JsonSerializer.DeserializeAsync<AsyncIoApiResponseDto>(bodyStream, _jsonOptions, cancellationToken).ConfigureAwait(false);

        if (payload is null)
        {
            throw new InvalidOperationException("Server returned an empty response payload.");
        }

        return payload;
    }

    private static AsyncIoBatchProcessingResult BuildResult(int expectedRequestCount, IEnumerable<AsyncIoApiResponseDto> responses, AsyncIoServerSnapshot snapshot)
    {
        AsyncIoApiResponseDto[] ordered = responses
            .OrderBy(x => x.RequestId, StringComparer.Ordinal)
            .ToArray();

        int completedStatusCount = ordered.Count(x => string.Equals(x.Status, "Completed", StringComparison.Ordinal));
        int totalAvailableUnits = ordered.Sum(x => x.AvailableUnits);

        long checksum = 17;
        foreach (AsyncIoApiResponseDto response in ordered)
        {
            checksum = unchecked((checksum * 31) + response.ChecksumComponent);
        }

        return new AsyncIoBatchProcessingResult(
            ExpectedRequestCount: expectedRequestCount,
            CompletedRequestCount: ordered.Length,
            SuccessfulResponseCount: ordered.Length,
            FailedResponseCount: Math.Max(0, expectedRequestCount - ordered.Length),
            CompletedStatusCount: completedStatusCount,
            TotalAvailableUnits: totalAvailableUnits,
            DeterministicChecksum: checksum,
            ServerTotalRequests: snapshot.TotalRequests,
            ServerActiveRequests: snapshot.ActiveRequests,
            ServerMaxObservedConcurrentRequests: snapshot.MaxObservedConcurrentRequests,
            ServerFailedRequests: snapshot.FailedRequests);
    }
}
