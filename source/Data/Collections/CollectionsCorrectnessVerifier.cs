using DotNetPerformanceLab.Models;
using DotNetPerformanceLab.Reporting.Collections;

namespace DotNetPerformanceLab.Data.Collections;

public sealed class CollectionsCorrectnessVerifier
{
    private readonly CollectionsAggregationService _aggregationService;

    public CollectionsCorrectnessVerifier(CollectionsAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    public CollectionsCorrectnessVerificationResult VerifyAllEquivalent(IReadOnlyList<RetailTransaction> transactions, decimal decimalTolerance = 0.01m)
    {
        Dictionary<CollectionsAggregationApproach, SalesSummary> results = new()
        {
            [CollectionsAggregationApproach.Linq] = _aggregationService.AggregateWithLinq(transactions),
            [CollectionsAggregationApproach.Foreach] = _aggregationService.AggregateWithForeach(transactions),
            [CollectionsAggregationApproach.Dictionary] = _aggregationService.AggregateWithDictionary(transactions),
            [CollectionsAggregationApproach.ParallelFor] = _aggregationService.AggregateWithParallelFor(transactions),
            [CollectionsAggregationApproach.Plinq] = _aggregationService.AggregateWithPlinq(transactions)
        };

        SalesSummary baseline = results[CollectionsAggregationApproach.Foreach];
        foreach ((CollectionsAggregationApproach approach, SalesSummary candidate) in results)
        {
            AssertEquivalent(baseline, candidate, approach, decimalTolerance);
        }

        return new CollectionsCorrectnessVerificationResult(results, decimalTolerance);
    }

    private static void AssertEquivalent(SalesSummary baseline, SalesSummary candidate, CollectionsAggregationApproach approach, decimal decimalTolerance)
    {
        if (baseline.TotalTransactionCount != candidate.TotalTransactionCount)
        {
            throw new InvalidOperationException($"TotalTransactionCount mismatch for {approach}.");
        }

        if (baseline.TotalUnitsSold != candidate.TotalUnitsSold)
        {
            throw new InvalidOperationException($"TotalUnitsSold mismatch for {approach}.");
        }

        if (Math.Abs(baseline.GrossSales - candidate.GrossSales) > decimalTolerance
            || Math.Abs(baseline.TotalDiscounts - candidate.TotalDiscounts) > decimalTolerance
            || Math.Abs(baseline.NetSales - candidate.NetSales) > decimalTolerance)
        {
            throw new InvalidOperationException($"Top-level monetary mismatch for {approach}.");
        }

        if (baseline.CategorySales.Count != candidate.CategorySales.Count)
        {
            throw new InvalidOperationException($"CategorySales count mismatch for {approach}.");
        }

        for (int index = 0; index < baseline.CategorySales.Count; index++)
        {
            CategorySalesSummary expected = baseline.CategorySales[index];
            CategorySalesSummary actual = candidate.CategorySales[index];

            if (!string.Equals(expected.Category, actual.Category, StringComparison.Ordinal)
                || expected.TransactionCount != actual.TransactionCount
                || expected.TotalUnits != actual.TotalUnits)
            {
                throw new InvalidOperationException($"Category identity/count mismatch for {approach}, category index {index}.");
            }

            if (Math.Abs(expected.GrossSales - actual.GrossSales) > decimalTolerance
                || Math.Abs(expected.TotalDiscounts - actual.TotalDiscounts) > decimalTolerance
                || Math.Abs(expected.NetSales - actual.NetSales) > decimalTolerance)
            {
                throw new InvalidOperationException($"Category monetary mismatch for {approach}, category {expected.Category}.");
            }
        }

        if (baseline.CategoryTransactionCounts.Count != candidate.CategoryTransactionCounts.Count)
        {
            throw new InvalidOperationException($"CategoryTransactionCounts count mismatch for {approach}.");
        }

        for (int index = 0; index < baseline.CategoryTransactionCounts.Count; index++)
        {
            (string expectedCategory, int expectedCount) = baseline.CategoryTransactionCounts[index];
            (string actualCategory, int actualCount) = candidate.CategoryTransactionCounts[index];

            if (!string.Equals(expectedCategory, actualCategory, StringComparison.Ordinal) || expectedCount != actualCount)
            {
                throw new InvalidOperationException($"CategoryTransactionCounts mismatch for {approach}, index {index}.");
            }
        }
    }
}
