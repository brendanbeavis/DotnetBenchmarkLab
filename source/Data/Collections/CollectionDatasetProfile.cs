namespace DotNetPerformanceLab.Data.Collections;

public sealed record CollectionDatasetProfile(
    int TransactionCount,
    int ProductCount,
    int CategoryCardinality,
    DateTime BaseDateUtc,
    int DateRangeDays);
