namespace DotNetPerformanceLab.Reporting.Collections;

public sealed record SalesSummary(
    int TotalTransactionCount,
    int TotalUnitsSold,
    decimal GrossSales,
    decimal TotalDiscounts,
    decimal NetSales,
    IReadOnlyList<CategorySalesSummary> CategorySales,
    IReadOnlyList<(string Category, int TransactionCount)> CategoryTransactionCounts);
