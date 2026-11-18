using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Tests;

public class EngineeringTradeoffCatalogTests
{
    [Fact]
    public void Catalog_CoversAllImplementedBenchmarkMethods()
    {
        string[] expectedMethods =
        [
            "EfCoreTracking",
            "EfCoreNoTracking",
            "EfCoreProjection",
            "EfCoreCompiledProjection",
            "Dapper",
            "AdoNet",
            "Linq",
            "Foreach",
            "PresizedDictionary",
            "CollectionsMarshal",
            "ParallelFor",
            "Plinq",
            "NewtonsoftParserOnly",
            "SystemTextJsonParserOnly",
            "SystemTextJsonSourceGenParserOnly",
            "SystemTextJsonStreamingParserOnlyStreamInput",
            "NewtonsoftEndToEnd",
            "SystemTextJsonEndToEnd",
            "SystemTextJsonSourceGenEndToEnd",
            "SystemTextJsonStreamingEndToEndStreamInput"
        ];

        foreach (string method in expectedMethods)
        {
            Assert.True(EngineeringTradeoffCatalog.TryGetByMethod(method, out EngineeringTradeoffInfo? info));
            Assert.NotNull(info);
        }
    }

    [Fact]
    public void Catalog_EntriesContainRequiredFields()
    {
        IReadOnlyList<EngineeringTradeoffInfo> entries = EngineeringTradeoffCatalog.GetAll();
        Assert.NotEmpty(entries);

        foreach (EngineeringTradeoffInfo entry in entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.HowItWorks));
            Assert.False(string.IsNullOrWhiteSpace(entry.Strengths));
            Assert.False(string.IsNullOrWhiteSpace(entry.CostsAndRisks));
            Assert.False(string.IsNullOrWhiteSpace(entry.WhenToUse));
            Assert.False(string.IsNullOrWhiteSpace(entry.WhenNotToUse));
            Assert.False(string.IsNullOrWhiteSpace(entry.WhatThisBenchmarkMeasures));
        }
    }

    [Fact]
    public void Catalog_AvoidsUniversalWinnerLanguage()
    {
        IReadOnlyList<EngineeringTradeoffInfo> entries = EngineeringTradeoffCatalog.GetAll();

        foreach (EngineeringTradeoffInfo entry in entries)
        {
            string combined = string.Join(' ', [entry.HowItWorks, entry.Strengths, entry.CostsAndRisks, entry.WhenToUse, entry.WhenNotToUse, entry.WhatThisBenchmarkMeasures]);
            Assert.DoesNotContain("always fastest", combined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("universally best", combined, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("never use", combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Catalog_ParallelEntriesClearlyDescribeCpuBoundTradeoffs()
    {
        EngineeringTradeoffInfo parallelFor = EngineeringTradeoffCatalog.GetByMethod("ParallelFor");
        EngineeringTradeoffInfo plinq = EngineeringTradeoffCatalog.GetByMethod("Plinq");

        Assert.Contains("CPU-bound", parallelFor.WhenToUse, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CPU-bound", plinq.WhenToUse, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_JsonStreamingAndSourceGenerationNotesAreAccurate()
    {
        EngineeringTradeoffInfo streaming = EngineeringTradeoffCatalog.GetByMethod("SystemTextJsonStreamingEndToEndStreamInput");
        EngineeringTradeoffInfo sourceGen = EngineeringTradeoffCatalog.GetByMethod("SystemTextJsonSourceGenEndToEnd");

        Assert.Contains("output collection still grows", streaming.CostsAndRisks, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not remove", sourceGen.CostsAndRisks, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("materialization", sourceGen.CostsAndRisks, StringComparison.OrdinalIgnoreCase);
    }
}
