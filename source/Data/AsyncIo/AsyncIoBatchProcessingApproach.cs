namespace DotNetPerformanceLab.Data.AsyncIo;

public enum AsyncIoBatchProcessingApproach
{
    Sequential,
    TaskWhenAll,
    SemaphoreSlimBounded,
    ParallelForEachAsync
}