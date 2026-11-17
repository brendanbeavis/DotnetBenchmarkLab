namespace DotNetPerformanceLab.Reporting.Json;

public sealed record JsonParserOnlyResultDto(
    int OrderCount,
    int TotalLineItemCount,
    int TotalUnits,
    decimal GrossSales,
    decimal DiscountTotal,
    decimal NetSales);
