namespace DotNetPerformanceLab.Reporting.Collections;

public sealed record CategorySalesSummary(
    string Category,
    int TransactionCount,
    int TotalUnits,
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal NetSales);
