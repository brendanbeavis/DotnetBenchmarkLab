namespace DotNetPerformanceLab.Reporting.Database;

public sealed record DatabaseOrderLineReportDto(
    int ProductId,
    string Sku,
    string ProductName,
    string Category,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal);
