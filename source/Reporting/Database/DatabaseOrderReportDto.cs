namespace DotNetPerformanceLab.Reporting.Database;

public sealed record DatabaseOrderReportDto(
    int OrderId,
    DateTime OrderDate,
    string Status,
    int CustomerId,
    string CustomerName,
    string CustomerEmail,
    string Region,
    int TotalQuantity,
    decimal OrderTotal,
    IReadOnlyList<DatabaseOrderLineReportDto> Lines);
