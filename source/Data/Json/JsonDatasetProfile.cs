namespace DotNetPerformanceLab.Data.Json;

public sealed record JsonDatasetProfile(
    int OrderCount,
    int CustomerCount,
    int ProductCount,
    int MinLineItemsPerOrder,
    int MaxLineItemsPerOrder,
    DateTime BaseDateUtc);
