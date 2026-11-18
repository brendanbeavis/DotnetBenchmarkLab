using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Data.Collections;

public static class CollectionDatasetProfiles
{
    public static CollectionDatasetProfile Get(DatasetSize datasetSize) => datasetSize switch
    {
        DatasetSize.Small => new CollectionDatasetProfile(
            TransactionCount: 10_000,
            ProductCount: 2_000,
            CategoryCardinality: 8,
            BaseDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateRangeDays: 180),
        DatasetSize.Medium => new CollectionDatasetProfile(
            TransactionCount: 250_000,
            ProductCount: 20_000,
            CategoryCardinality: 8,
            BaseDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateRangeDays: 180),
        DatasetSize.Large => new CollectionDatasetProfile(
            TransactionCount: 1_000_000,
            ProductCount: 100_000,
            CategoryCardinality: 8,
            BaseDateUtc: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DateRangeDays: 180),
        _ => throw new ArgumentOutOfRangeException(nameof(datasetSize), datasetSize, "Unsupported dataset size.")
    };
}
