using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.Json;

public static class JsonDatasetProfiles
{
    public static JsonDatasetProfile Get(DatasetSize datasetSize) => datasetSize switch
    {
        DatasetSize.Small => new JsonDatasetProfile(
            OrderCount: 1_000,
            CustomerCount: 300,
            ProductCount: 700,
            MinLineItemsPerOrder: 1,
            MaxLineItemsPerOrder: 4,
            BaseDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
        DatasetSize.Medium => new JsonDatasetProfile(
            OrderCount: 50_000,
            CustomerCount: 7_500,
            ProductCount: 6_000,
            MinLineItemsPerOrder: 1,
            MaxLineItemsPerOrder: 5,
            BaseDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
        DatasetSize.Large => new JsonDatasetProfile(
            OrderCount: 250_000,
            CustomerCount: 20_000,
            ProductCount: 20_000,
            MinLineItemsPerOrder: 1,
            MaxLineItemsPerOrder: 6,
            BaseDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
        _ => throw new ArgumentOutOfRangeException(nameof(datasetSize), datasetSize, "Unsupported dataset size.")
    };
}
