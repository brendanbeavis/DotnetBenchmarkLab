using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.AsyncIo;

public static class AsyncIoDatasetProfiles
{
    public static AsyncIoDatasetProfile Get(DatasetSize datasetSize) => datasetSize switch
    {
        DatasetSize.Small => new AsyncIoDatasetProfile(DatasetSize.Small, RequestCount: 24, SimulatedLatencyMilliseconds: 6, BoundedConcurrencyLimit: 4, StoreCount: 6),
        DatasetSize.Medium => new AsyncIoDatasetProfile(DatasetSize.Medium, RequestCount: 72, SimulatedLatencyMilliseconds: 6, BoundedConcurrencyLimit: 8, StoreCount: 12),
        DatasetSize.Large => new AsyncIoDatasetProfile(DatasetSize.Large, RequestCount: 160, SimulatedLatencyMilliseconds: 6, BoundedConcurrencyLimit: 16, StoreCount: 20),
        _ => throw new ArgumentOutOfRangeException(nameof(datasetSize), datasetSize, "Unsupported dataset size.")
    };
}