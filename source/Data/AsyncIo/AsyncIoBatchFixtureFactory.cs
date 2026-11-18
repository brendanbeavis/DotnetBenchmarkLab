using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.AsyncIo;

public static class AsyncIoBatchFixtureFactory
{
    public static async Task<AsyncIoBatchFixture> CreateAsync(DatasetSize datasetSize, CancellationToken cancellationToken = default)
    {
        AsyncIoDatasetProfile profile = AsyncIoDatasetProfiles.Get(datasetSize);
        IReadOnlyList<AsyncIoRequestKey> batch = AsyncIoRequestBatchFactory.Create(profile);
        LocalRetailApiTestServer server = new(profile.SimulatedLatencyMilliseconds);

        try
        {
            await server.StartAsync(cancellationToken).ConfigureAwait(false);

            HttpClient client = new()
            {
                BaseAddress = server.BaseAddress,
                Timeout = TimeSpan.FromSeconds(30)
            };

            return new AsyncIoBatchFixture(datasetSize, profile, batch, server, client);
        }
        catch
        {
            await server.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
