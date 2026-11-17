namespace DotNetPerformanceLab.Data.Database;

public sealed record RetailDatasetProfile(
    int CustomerCount,
    int ProductCount,
    int OrderCount,
    int MinLinesPerOrder,
    int MaxLinesPerOrder,
    DateTime BaseOrderDateUtc,
    int QueryWindowStartDayOffset,
    int QueryWindowLengthDays);
