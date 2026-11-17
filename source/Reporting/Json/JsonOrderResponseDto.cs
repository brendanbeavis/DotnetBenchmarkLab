namespace DotNetPerformanceLab.Reporting.Json;

public sealed record JsonOrderResponseDto(
    int OrderId,
    int CustomerId,
    string CustomerName,
    string Status,
    DateTime OrderDateUtc,
    int TotalUnits,
    decimal GrossTotal,
    decimal DiscountTotal,
    decimal NetTotal,
    int DistinctProductCount);
