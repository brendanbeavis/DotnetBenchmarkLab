namespace DotNetPerformanceLab.Data.Collections;

public sealed record CollectionDatasetProfile(
    int TransactionCount,
    int ProductCount,
    DateTime BaseDateUtc,
    int DateRangeDays);
