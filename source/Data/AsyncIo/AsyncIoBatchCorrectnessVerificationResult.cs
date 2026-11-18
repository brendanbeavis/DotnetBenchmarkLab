namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed record AsyncIoBatchCorrectnessVerificationResult(
    IReadOnlyDictionary<AsyncIoBatchProcessingApproach, AsyncIoBatchProcessingResult> ResultsByApproach,
    int ExpectedRequestCount,
    int BoundedConcurrencyLimit);