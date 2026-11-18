using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Data.Generators;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class CollectionsScenarioCorrectnessValidator : IScenarioCorrectnessValidator
{
    public BenchmarkScenarioId ScenarioId => BenchmarkScenarioId.Collections;

    public ScenarioPreflightResult Validate(DatasetSize datasetSize, JsonBenchmarkGroup jsonGroup)
    {
        CollectionDatasetProfile profile = CollectionDatasetProfiles.Get(datasetSize);
        CollectionDatasetFixture fixture = CollectionDatasetGenerator.Create(datasetSize);
        CollectionsAggregationService aggregationService = new();
        CollectionsCorrectnessVerifier verifier = new(aggregationService);
        CollectionsCorrectnessVerificationResult verification = verifier.VerifyAllEquivalent(fixture.Transactions, profile.CategoryCardinality);

        IReadOnlyList<ImplementationPreflightResult> implementations = verification.ResultsByApproach
            .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
            .Select(pair => new ImplementationPreflightResult(
                pair.Key.ToString(),
                Passed: true,
                $"Transactions: {pair.Value.TotalTransactionCount:N0}, Categories: {pair.Value.CategorySales.Count:N0}"))
            .ToArray();

        return new ScenarioPreflightResult(
            ScenarioId,
            "Collections — Retail transaction aggregation",
            profile.TransactionCount,
            ScenarioPreflightStatus.Passed,
            $"Validated {implementations.Count} implementations with decimal tolerance {verification.DecimalTolerance:0.##}.",
            implementations);
    }
}
