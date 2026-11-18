using DotNetPerformanceLab.Data.Database;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class DatabaseScenarioCorrectnessValidator : IScenarioCorrectnessValidator
{
    public BenchmarkScenarioId ScenarioId => BenchmarkScenarioId.Database;

    public ScenarioPreflightResult Validate(DatasetSize datasetSize, JsonBenchmarkGroup jsonGroup)
    {
        using RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(datasetSize);
        DatabaseReportingQueryService queryService = new(fixture);
        DatabaseCorrectnessVerifier verifier = new(queryService);
        DatabaseCorrectnessVerificationResult verification = verifier.VerifyAllEquivalent(fixture.ReportQuery);

        IReadOnlyList<ImplementationPreflightResult> implementations = verification.ResultsByApproach
            .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
            .Select(pair => new ImplementationPreflightResult(pair.Key.ToString(), Passed: true, $"Rows: {pair.Value.Count:N0}"))
            .ToArray();

        int approxRecords = RetailDatasetProfiles.Get(datasetSize).OrderCount;

        return new ScenarioPreflightResult(
            ScenarioId,
            "Database — Retail order reporting",
            approxRecords,
            ScenarioPreflightStatus.Passed,
            $"Validated {implementations.Count} implementations with decimal tolerance {verification.DecimalTolerance:0.##}.",
            implementations);
    }
}
