namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed record AsyncIoRequestKey(
    string RequestId,
    int StoreId,
    int OrderId);
