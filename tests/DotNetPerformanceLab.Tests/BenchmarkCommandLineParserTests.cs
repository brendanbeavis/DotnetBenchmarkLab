using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Tests;

public class BenchmarkCommandLineParserTests
{
    [Fact]
    public void TryParse_NoArguments_ReturnsInteractiveDefaults()
    {
        bool parsed = BenchmarkCommandLineParser.TryParse([], out BenchmarkRunOptions options, out string? error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.False(options.IsBenchmarkMode);
        Assert.Equal(DatasetSize.Medium, options.DatasetSize);
        Assert.False(options.SkipPreflight);
    }

    [Fact]
    public void TryParse_BenchmarkScenarioAndDataset_ReturnsExpectedOptions()
    {
        bool parsed = BenchmarkCommandLineParser.TryParse(["--benchmark", "database", "--dataset", "large"], out BenchmarkRunOptions options, out string? error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.True(options.IsBenchmarkMode);
        Assert.False(options.RunAllScenarios);
        Assert.Equal(BenchmarkScenarioId.Database, options.ScenarioId);
        Assert.Equal(DatasetSize.Large, options.DatasetSize);
    }

    [Theory]
    [InlineData(new[] { "--benchmark", "all", "--dataset", "medium" }, true, null, DatasetSize.Medium)]
    [InlineData(new[] { "--benchmark", "database", "--dataset", "small" }, false, BenchmarkScenarioId.Database, DatasetSize.Small)]
    [InlineData(new[] { "--benchmark", "collections", "--dataset", "medium" }, false, BenchmarkScenarioId.Collections, DatasetSize.Medium)]
    [InlineData(new[] { "--benchmark", "json", "--dataset", "large" }, false, BenchmarkScenarioId.Json, DatasetSize.Large)]
    public void TryParse_DocumentedBenchmarkCommands_ParseSuccessfully(string[] args, bool expectedRunAll, BenchmarkScenarioId? expectedScenario, DatasetSize expectedDataset)
    {
        bool parsed = BenchmarkCommandLineParser.TryParse(args, out BenchmarkRunOptions options, out string? error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.True(options.IsBenchmarkMode);
        Assert.Equal(expectedRunAll, options.RunAllScenarios);
        Assert.Equal(expectedScenario, options.ScenarioId);
        Assert.Equal(expectedDataset, options.DatasetSize);
    }

    [Theory]
    [InlineData("all", JsonBenchmarkGroup.All)]
    [InlineData("parser", JsonBenchmarkGroup.ParserOnly)]
    [InlineData("endtoend", JsonBenchmarkGroup.EndToEnd)]
    public void TryParse_JsonGroup_IsParsed(string jsonGroup, JsonBenchmarkGroup expected)
    {
        bool parsed = BenchmarkCommandLineParser.TryParse(["--benchmark", "json", "--dataset", "small", "--json-group", jsonGroup], out BenchmarkRunOptions options, out string? error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.Equal(expected, options.JsonGroup);
    }

    [Fact]
    public void TryParse_InvalidJsonGroup_ReturnsError()
    {
        bool parsed = BenchmarkCommandLineParser.TryParse(["--benchmark", "json", "--dataset", "small", "--json-group", "weird"], out _, out string? error);

        Assert.False(parsed);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_InvalidDataset_ReturnsError()
    {
        bool parsed = BenchmarkCommandLineParser.TryParse(["--benchmark", "json", "--dataset", "huge"], out _, out string? error);

        Assert.False(parsed);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryParse_SkipPreflightFlag_IsParsed()
    {
        bool parsed = BenchmarkCommandLineParser.TryParse(["--benchmark", "collections", "--dataset", "small", "--skip-preflight"], out BenchmarkRunOptions options, out string? error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.True(options.SkipPreflight);
    }
}
