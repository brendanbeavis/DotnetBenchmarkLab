using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Models;

namespace DotNetPerformanceLab.Tests;

public class CollectionScenarioTests
{
    [Fact]
    public void Generator_IsDeterministic_ForSmallDataset()
    {
        CollectionDatasetFixture first = CollectionDatasetGenerator.Create(DatasetSize.Small);
        CollectionDatasetFixture second = CollectionDatasetGenerator.Create(DatasetSize.Small);

        Assert.Equal(first.Transactions.Count, second.Transactions.Count);
        for (int index = 0; index < 25; index++)
        {
            Assert.Equal(first.Transactions[index], second.Transactions[index]);
        }
    }

    [Fact]
    public void HandAuthoredFixture_ProducesExpectedSummary()
    {
        IReadOnlyList<RetailTransaction> transactions =
        [
            new RetailTransaction(1, 100, "Apparel", 2, 50m, 0.10m, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            new RetailTransaction(2, 101, "Apparel", 1, 100m, 0m, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            new RetailTransaction(3, 200, "Electronics", 3, 20m, 0.05m, new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc))
        ];

        CollectionsAggregationService service = new();
        var summary = service.AggregateWithForeach(transactions);

        Assert.Equal(3, summary.TotalTransactionCount);
        Assert.Equal(6, summary.TotalUnitsSold);
        Assert.Equal(260m, summary.GrossSales);
        Assert.Equal(13m, summary.TotalDiscounts);
        Assert.Equal(247m, summary.NetSales);

        Assert.Collection(summary.CategorySales,
            apparel =>
            {
                Assert.Equal("Apparel", apparel.Category);
                Assert.Equal(2, apparel.TransactionCount);
                Assert.Equal(3, apparel.TotalUnits);
                Assert.Equal(200m, apparel.GrossSales);
                Assert.Equal(10m, apparel.TotalDiscounts);
                Assert.Equal(190m, apparel.NetSales);
            },
            electronics =>
            {
                Assert.Equal("Electronics", electronics.Category);
                Assert.Equal(1, electronics.TransactionCount);
                Assert.Equal(3, electronics.TotalUnits);
                Assert.Equal(60m, electronics.GrossSales);
                Assert.Equal(3m, electronics.TotalDiscounts);
                Assert.Equal(57m, electronics.NetSales);
            });
    }

    [Fact]
    public void AllImplementations_ReturnEquivalentResults()
    {
        CollectionDatasetFixture fixture = CollectionDatasetGenerator.Create(DatasetSize.Small);
        CollectionDatasetProfile profile = CollectionDatasetProfiles.Get(DatasetSize.Small);
        CollectionsAggregationService service = new();
        CollectionsCorrectnessVerifier verifier = new(service);

        CollectionsCorrectnessVerificationResult result = verifier.VerifyAllEquivalent(fixture.Transactions, profile.CategoryCardinality);

        Assert.Equal(6, result.ResultsByApproach.Count);
    }

    [Fact]
    public void EmptyInput_AllApproachesMatchReference()
    {
        AssertAllApproachesMatchReference([], estimatedCategoryCardinality: 0);
    }

    [Fact]
    public void SingleItem_AllApproachesMatchReference()
    {
        IReadOnlyList<RetailTransaction> transactions =
        [
            new RetailTransaction(1, 100, "Apparel", 2, 19.95m, 0.10m, DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc))
        ];

        AssertAllApproachesMatchReference(transactions, estimatedCategoryCardinality: 1);
    }

    [Fact]
    public void MultipleCategory_AllApproachesMatchReference()
    {
        IReadOnlyList<RetailTransaction> transactions =
        [
            new RetailTransaction(1, 100, "Apparel", 1, 10m, 0.10m, DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc)),
            new RetailTransaction(2, 101, "Electronics", 2, 20m, 0.05m, DateTime.SpecifyKind(new DateTime(2024, 1, 2), DateTimeKind.Utc)),
            new RetailTransaction(3, 102, "Books", 3, 30m, 0m, DateTime.SpecifyKind(new DateTime(2024, 1, 3), DateTimeKind.Utc)),
            new RetailTransaction(4, 103, "Apparel", 1, 12m, 0m, DateTime.SpecifyKind(new DateTime(2024, 1, 4), DateTimeKind.Utc))
        ];

        AssertAllApproachesMatchReference(transactions, estimatedCategoryCardinality: 3);
    }

    [Fact]
    public void ZeroDiscount_AllApproachesMatchReference()
    {
        IReadOnlyList<RetailTransaction> transactions =
        [
            new RetailTransaction(1, 100, "Apparel", 2, 20m, 0m, DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc)),
            new RetailTransaction(2, 101, "Electronics", 1, 15m, 0m, DateTime.SpecifyKind(new DateTime(2024, 1, 2), DateTimeKind.Utc)),
            new RetailTransaction(3, 102, "Apparel", 3, 5m, 0m, DateTime.SpecifyKind(new DateTime(2024, 1, 3), DateTimeKind.Utc))
        ];

        AssertAllApproachesMatchReference(transactions, estimatedCategoryCardinality: 2);
    }

    [Fact]
    public void FractionalMonetary_AllApproachesMatchReference()
    {
        IReadOnlyList<RetailTransaction> transactions =
        [
            new RetailTransaction(1, 100, "Apparel", 3, 12.34m, 0.07m, DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc)),
            new RetailTransaction(2, 101, "Apparel", 1, 99.99m, 0.13m, DateTime.SpecifyKind(new DateTime(2024, 1, 2), DateTimeKind.Utc)),
            new RetailTransaction(3, 102, "Books", 2, 7.77m, 0.19m, DateTime.SpecifyKind(new DateTime(2024, 1, 3), DateTimeKind.Utc))
        ];

        AssertAllApproachesMatchReference(transactions, estimatedCategoryCardinality: 2);
    }

    [Fact]
    public void BoundaryValues_AreHandledConsistently()
    {
        IReadOnlyList<RetailTransaction> transactions =
        [
            new RetailTransaction(1, 1, "A", 0, 10m, 0m, DateTime.UtcNow),
            new RetailTransaction(2, 2, "A", 5, 10m, 0m, DateTime.UtcNow),
            new RetailTransaction(3, 3, "B", 1, 999_999.99m, 0.20m, DateTime.UtcNow)
        ];

        CollectionsAggregationService service = new();
        CollectionsCorrectnessVerifier verifier = new(service);

        CollectionsCorrectnessVerificationResult verification = verifier.VerifyAllEquivalent(transactions, estimatedCategoryCardinality: 2);

        var summary = verification.ResultsByApproach[CollectionsAggregationApproach.Foreach];
        Assert.Equal(3, summary.TotalTransactionCount);
        Assert.Equal(6, summary.TotalUnitsSold);
        Assert.Equal(1_000_049.99m, summary.GrossSales);
        Assert.Equal(200_000.00m, summary.TotalDiscounts);
        Assert.Equal(800_049.99m, summary.NetSales);
    }

    [Fact]
    public void CategoryOrdering_IsDeterministic()
    {
        IReadOnlyList<RetailTransaction> transactions =
        [
            new RetailTransaction(1, 1, "Z", 1, 10m, 0m, DateTime.UtcNow),
            new RetailTransaction(2, 2, "A", 1, 10m, 0m, DateTime.UtcNow),
            new RetailTransaction(3, 3, "M", 1, 10m, 0m, DateTime.UtcNow)
        ];

        CollectionsAggregationService service = new();
        var summary = service.AggregateWithPresizedDictionary(transactions, estimatedCategoryCount: 3);

        Assert.Equal(["A", "M", "Z"], summary.CategorySales.Select(x => x.Category).ToArray());
        Assert.Equal(["A", "M", "Z"], summary.CategoryTransactionCounts.Select(x => x.Category).ToArray());
    }

    [Theory]
    [InlineData(DatasetSize.Small, 10_000)]
    [InlineData(DatasetSize.Medium, 250_000)]
    [InlineData(DatasetSize.Large, 1_000_000)]
    public void DatasetProfiles_MapExpectedCounts(DatasetSize datasetSize, int expected)
    {
        CollectionDatasetProfile profile = CollectionDatasetProfiles.Get(datasetSize);
        Assert.Equal(expected, profile.TransactionCount);
    }

    [Theory]
    [InlineData(DatasetSize.Small, 8)]
    [InlineData(DatasetSize.Medium, 8)]
    [InlineData(DatasetSize.Large, 8)]
    public void DatasetProfiles_ExposeCategoryCardinality(DatasetSize datasetSize, int expected)
    {
        CollectionDatasetProfile profile = CollectionDatasetProfiles.Get(datasetSize);
        Assert.Equal(expected, profile.CategoryCardinality);
    }

    private static void AssertAllApproachesMatchReference(IReadOnlyList<RetailTransaction> transactions, int estimatedCategoryCardinality)
    {
        CollectionsAggregationService service = new();
        CollectionsCorrectnessVerifier verifier = new(service);
        CollectionsCorrectnessVerificationResult verification = verifier.VerifyAllEquivalent(transactions, estimatedCategoryCardinality);

        Assert.Equal(6, verification.ResultsByApproach.Count);
    }
}
