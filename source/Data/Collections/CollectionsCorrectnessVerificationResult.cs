using DotNetPerformanceLab.Reporting.Collections;

namespace DotNetPerformanceLab.Data.Collections;

public sealed record CollectionsCorrectnessVerificationResult(
    IReadOnlyDictionary<CollectionsAggregationApproach, SalesSummary> ResultsByApproach,
    decimal DecimalTolerance);
