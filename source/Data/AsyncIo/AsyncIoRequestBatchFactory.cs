using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.AsyncIo;

public static class AsyncIoRequestBatchFactory
{
    public static IReadOnlyList<AsyncIoRequestKey> Create(DatasetSize datasetSize)
    {
        AsyncIoDatasetProfile profile = AsyncIoDatasetProfiles.Get(datasetSize);
        return Create(profile);
    }

    public static IReadOnlyList<AsyncIoRequestKey> Create(AsyncIoDatasetProfile profile)
    {
        List<AsyncIoRequestKey> requests = new(profile.RequestCount);

        string datasetLabel = profile.DatasetSize.ToString().ToUpperInvariant();
        for (int index = 0; index < profile.RequestCount; index++)
        {
            int storeId = 100 + (index % profile.StoreCount);
            int orderId = 10_000 + index;
            string requestId = $"REQ-{datasetLabel}-{index:D4}";
            requests.Add(new AsyncIoRequestKey(requestId, storeId, orderId));
        }

        return requests;
    }
}
