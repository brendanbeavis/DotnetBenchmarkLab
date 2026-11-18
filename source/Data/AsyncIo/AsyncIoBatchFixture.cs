using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed class AsyncIoBatchFixture : IAsyncDisposable
{
    public AsyncIoBatchFixture(
        DatasetSize datasetSize,
        AsyncIoDatasetProfile profile,
        IReadOnlyList<AsyncIoRequestKey> requestBatch,
        LocalRetailApiTestServer server,
        HttpClient httpClient)
    {
        DatasetSize = datasetSize;
        Profile = profile;
        RequestBatch = requestBatch;
        Server = server;
        HttpClient = httpClient;
    }

    public DatasetSize DatasetSize { get; }

    public AsyncIoDatasetProfile Profile { get; }

    public IReadOnlyList<AsyncIoRequestKey> RequestBatch { get; }

    public LocalRetailApiTestServer Server { get; }

    public HttpClient HttpClient { get; }

    public ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        return Server.DisposeAsync();
    }
}