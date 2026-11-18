using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Data.Json;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class JsonScenarioCorrectnessValidator : IScenarioCorrectnessValidator
{
    public BenchmarkScenarioId ScenarioId => BenchmarkScenarioId.Json;

    public ScenarioPreflightResult Validate(DatasetSize datasetSize, JsonBenchmarkGroup jsonGroup)
    {
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(datasetSize);
        JsonPayloadProcessingService processingService = new();
        JsonCorrectnessVerifier verifier = new(processingService);
        JsonCorrectnessVerificationResult verification = verifier.VerifyAllEquivalent(fixture.PayloadUtf8);

        List<ImplementationPreflightResult> implementations = [];
        if (jsonGroup is JsonBenchmarkGroup.All or JsonBenchmarkGroup.ParserOnly)
        {
            implementations.AddRange(verification.ParserOnlyResults
                .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
                .Select(pair => new ImplementationPreflightResult(
                    $"ParserOnly:{pair.Key}",
                    Passed: true,
                    $"Orders: {pair.Value.OrderCount:N0}, Line items: {pair.Value.TotalLineItemCount:N0}")));
        }

        if (jsonGroup is JsonBenchmarkGroup.All or JsonBenchmarkGroup.EndToEnd)
        {
            implementations.AddRange(verification.EndToEndResults
                .OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)
                .Select(pair => new ImplementationPreflightResult(
                    $"EndToEnd:{pair.Key}",
                    Passed: true,
                    $"Orders: {pair.Value.OrderCount:N0}, Net sales: {pair.Value.NetSales:0.00}")));
        }

        int approxRecords = JsonDatasetProfiles.Get(datasetSize).OrderCount;

        return new ScenarioPreflightResult(
            ScenarioId,
            "JSON — Retail API payload processing",
            approxRecords,
            ScenarioPreflightStatus.Passed,
            $"Validated {implementations.Count} implementation checks with decimal tolerance {verification.DecimalTolerance:0.##}.",
            implementations);
    }
}
