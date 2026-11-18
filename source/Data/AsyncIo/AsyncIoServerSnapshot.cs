namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed record AsyncIoServerSnapshot(
    int TotalRequests,
    int ActiveRequests,
    int MaxObservedConcurrentRequests,
    int FailedRequests);