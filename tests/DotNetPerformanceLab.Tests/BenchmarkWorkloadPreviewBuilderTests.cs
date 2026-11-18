using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Data.Database;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Data.Json;

namespace DotNetPerformanceLab.Tests;

public class BenchmarkWorkloadPreviewBuilderTests
{
    [Theory]
    [InlineData(DatasetSize.Small)]
    [InlineData(DatasetSize.Medium)]
    [InlineData(DatasetSize.Large)]
    public void Build_DatabasePreview_UsesSelectedDatasetMetadata(DatasetSize datasetSize)
    {
        BenchmarkWorkloadPreviewBuilder builder = new();
        BenchmarkWorkloadPreview preview = builder.Build(CreateSingleScenarioOptions(BenchmarkScenarioId.Database, datasetSize));
        ScenarioWorkloadPreview database = GetScenario(preview, BenchmarkScenarioId.Database);

        RetailDatasetProfile profile = RetailDatasetProfiles.Get(datasetSize);
        int expectedLines = RetailDatabaseSeeder.CalculateDeterministicOrderLineCount(profile, BenchmarkConventions.DeterministicSeed);

        Assert.Equal(profile.CustomerCount.ToString("N0"), GetInputValue(database, "Customers"));
        Assert.Equal(profile.ProductCount.ToString("N0"), GetInputValue(database, "Products"));
        Assert.Equal(profile.OrderCount.ToString("N0"), GetInputValue(database, "Orders"));
        Assert.Equal(expectedLines.ToString("N0"), GetInputValue(database, "Order lines"));
    }

    [Theory]
    [InlineData(DatasetSize.Small)]
    [InlineData(DatasetSize.Medium)]
    [InlineData(DatasetSize.Large)]
    public void Build_CollectionsPreview_UsesSelectedDatasetMetadata(DatasetSize datasetSize)
    {
        BenchmarkWorkloadPreviewBuilder builder = new();
        BenchmarkWorkloadPreview preview = builder.Build(CreateSingleScenarioOptions(BenchmarkScenarioId.Collections, datasetSize));
        ScenarioWorkloadPreview collections = GetScenario(preview, BenchmarkScenarioId.Collections);

        CollectionDatasetProfile profile = CollectionDatasetProfiles.Get(datasetSize);

        Assert.Equal(profile.TransactionCount.ToString("N0"), GetInputValue(collections, "Transactions"));
        Assert.Equal(profile.ProductCount.ToString("N0"), GetInputValue(collections, "Products"));
        Assert.Equal(profile.CategoryCardinality.ToString("N0"), GetInputValue(collections, "Category cardinality"));
    }

    [Theory]
    [InlineData(DatasetSize.Small)]
    [InlineData(DatasetSize.Medium)]
    [InlineData(DatasetSize.Large)]
    public void Build_JsonPreview_UsesSelectedDatasetMetadata(DatasetSize datasetSize)
    {
        BenchmarkWorkloadPreviewBuilder builder = new();
        BenchmarkWorkloadPreview preview = builder.Build(CreateSingleScenarioOptions(BenchmarkScenarioId.Json, datasetSize, JsonBenchmarkGroup.All));
        ScenarioWorkloadPreview json = GetScenario(preview, BenchmarkScenarioId.Json);

        JsonDatasetProfile profile = JsonDatasetProfiles.Get(datasetSize);
        JsonPayloadFixture fixture = JsonPayloadFixtureGenerator.Create(datasetSize);

        Assert.Equal(profile.OrderCount.ToString("N0"), GetInputValue(json, "Orders"));
        Assert.Equal(fixture.TotalLineItemCount.ToString("N0"), GetInputValue(json, "Line items"));
        Assert.Equal(fixture.PayloadUtf8.Length.ToString("N0"), GetInputValue(json, "Payload UTF-8 bytes"));
    }

    [Fact]
    public void Build_JsonParserOnlyPreview_ContainsOnlyParserMethods()
    {
        BenchmarkWorkloadPreviewBuilder builder = new();
        BenchmarkWorkloadPreview preview = builder.Build(CreateSingleScenarioOptions(BenchmarkScenarioId.Json, DatasetSize.Small, JsonBenchmarkGroup.ParserOnly));
        ScenarioWorkloadPreview json = GetScenario(preview, BenchmarkScenarioId.Json);

        Assert.All(json.MethodsCompared, method =>
            Assert.True(
                method.MethodName.StartsWith("NewtonsoftParserOnly", StringComparison.OrdinalIgnoreCase)
                || method.MethodName.StartsWith("SystemTextJson", StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(json.MethodsCompared, method => method.MethodName.Contains("EndToEnd", StringComparison.Ordinal));
        Assert.Equal("ParserOnly", GetRunSettingValue(json, "JSON benchmark group"));
    }

    [Fact]
    public void Build_JsonEndToEndPreview_ContainsOnlyEndToEndMethods()
    {
        BenchmarkWorkloadPreviewBuilder builder = new();
        BenchmarkWorkloadPreview preview = builder.Build(CreateSingleScenarioOptions(BenchmarkScenarioId.Json, DatasetSize.Small, JsonBenchmarkGroup.EndToEnd));
        ScenarioWorkloadPreview json = GetScenario(preview, BenchmarkScenarioId.Json);

        Assert.All(json.MethodsCompared, method => Assert.Contains("EndToEnd", method.MethodName, StringComparison.Ordinal));
        Assert.DoesNotContain(json.MethodsCompared, method => method.MethodName.Contains("ParserOnly", StringComparison.Ordinal));
        Assert.Equal("EndToEnd", GetRunSettingValue(json, "JSON benchmark group"));
    }

    [Fact]
    public void Build_RunAllPreview_IncludesAllImplementedScenarios()
    {
        BenchmarkWorkloadPreviewBuilder builder = new();
        BenchmarkWorkloadPreview preview = builder.Build(new BenchmarkRunOptions(
            IsBenchmarkMode: true,
            RunAllScenarios: true,
            ScenarioId: null,
            DatasetSize: DatasetSize.Small,
            JsonGroup: JsonBenchmarkGroup.All,
            IsHelpRequested: false,
            SkipPreflight: false));

        int implementedCount = BenchmarkScenarioRegistry.GetAll().Count(s => s.IsImplemented);
        Assert.Equal(implementedCount, preview.Scenarios.Count);
    }

    [Fact]
    public void Build_MethodDescriptionsAndTradeoffs_ComeFromCatalog()
    {
        BenchmarkWorkloadPreviewBuilder builder = new();
        BenchmarkWorkloadPreview preview = builder.Build(CreateSingleScenarioOptions(BenchmarkScenarioId.Collections, DatasetSize.Small));
        ScenarioWorkloadPreview scenario = GetScenario(preview, BenchmarkScenarioId.Collections);

        foreach (BenchmarkMethodPreview method in scenario.MethodsCompared)
        {
            EngineeringTradeoffInfo info = EngineeringTradeoffCatalog.GetByMethod(method.MethodName);
            Assert.Equal(info.HowItWorks, method.Description);
            Assert.Contains("Use:", method.TradeoffNote, StringComparison.Ordinal);
            Assert.Contains("Risk:", method.TradeoffNote, StringComparison.Ordinal);
        }
    }

    private static BenchmarkRunOptions CreateSingleScenarioOptions(BenchmarkScenarioId scenarioId, DatasetSize datasetSize, JsonBenchmarkGroup jsonGroup = JsonBenchmarkGroup.All) => new(
        IsBenchmarkMode: true,
        RunAllScenarios: false,
        ScenarioId: scenarioId,
        DatasetSize: datasetSize,
        JsonGroup: jsonGroup,
        IsHelpRequested: false,
        SkipPreflight: false);

    private static ScenarioWorkloadPreview GetScenario(BenchmarkWorkloadPreview preview, BenchmarkScenarioId scenarioId)
    {
        return preview.Scenarios.Single(x => x.ScenarioId == scenarioId);
    }

    private static string GetInputValue(ScenarioWorkloadPreview preview, string label)
    {
        return preview.InputDataset.Single(x => string.Equals(x.Label, label, StringComparison.Ordinal)).Value;
    }

    private static string GetRunSettingValue(ScenarioWorkloadPreview preview, string label)
    {
        return preview.RunSettings.Single(x => string.Equals(x.Label, label, StringComparison.Ordinal)).Value;
    }
}
