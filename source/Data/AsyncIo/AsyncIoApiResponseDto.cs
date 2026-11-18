namespace DotNetPerformanceLab.Data.AsyncIo;

public sealed record AsyncIoApiResponseDto(
    string RequestId,
    int StoreId,
    int OrderId,
    string Status,
    int AvailableUnits,
    long ChecksumComponent);
