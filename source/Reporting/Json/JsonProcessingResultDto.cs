namespace DotNetPerformanceLab.Reporting.Json;

public sealed record JsonProcessingResultDto(
    int OrderCount,
    int TotalUnits,
    decimal GrossSales,
    decimal DiscountTotal,
    decimal NetSales,
    IReadOnlyList<JsonOrderResponseDto> Orders);
