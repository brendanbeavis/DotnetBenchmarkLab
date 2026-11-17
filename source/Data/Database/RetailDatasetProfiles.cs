using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.Database;

public static class RetailDatasetProfiles
{
    public static RetailDatasetProfile Get(DatasetSize datasetSize) => datasetSize switch
    {
        DatasetSize.Small => new RetailDatasetProfile(
            CustomerCount: 250,
            ProductCount: 400,
            OrderCount: 1_000,
            MinLinesPerOrder: 1,
            MaxLinesPerOrder: 4,
            BaseOrderDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            QueryWindowStartDayOffset: 300,
            QueryWindowLengthDays: 45),
        DatasetSize.Medium => new RetailDatasetProfile(
            CustomerCount: 5_000,
            ProductCount: 2_000,
            OrderCount: 50_000,
            MinLinesPerOrder: 1,
            MaxLinesPerOrder: 5,
            BaseOrderDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            QueryWindowStartDayOffset: 300,
            QueryWindowLengthDays: 45),
        DatasetSize.Large => new RetailDatasetProfile(
            CustomerCount: 20_000,
            ProductCount: 5_000,
            OrderCount: 250_000,
            MinLinesPerOrder: 1,
            MaxLinesPerOrder: 6,
            BaseOrderDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            QueryWindowStartDayOffset: 300,
            QueryWindowLengthDays: 45),
        _ => throw new ArgumentOutOfRangeException(nameof(datasetSize), datasetSize, "Unsupported dataset size.")
    };
}
