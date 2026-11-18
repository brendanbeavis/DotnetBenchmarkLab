namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed record AsyncIoBatchProcessingResult(
    int ExpectedRequestCount,
    int CompletedRequestCount,
    int SuccessfulResponseCount,
    int FailedResponseCount,
    int CompletedStatusCount,
    int TotalAvailableUnits,
    long DeterministicChecksum,
    int ServerTotalRequests,
    int ServerActiveRequests,
    int ServerMaxObservedConcurrentRequests,
    int ServerFailedRequests);
