using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed record AsyncIoDatasetProfile(
    DatasetSize DatasetSize,
    int RequestCount,
    int SimulatedLatencyMilliseconds,
    int BoundedConcurrencyLimit,
    int StoreCount);