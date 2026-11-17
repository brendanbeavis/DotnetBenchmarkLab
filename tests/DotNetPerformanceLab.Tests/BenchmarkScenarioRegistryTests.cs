using DotNetPerformanceLab.Benchmarking;

namespace DotNetPerformanceLab.Tests;

public class BenchmarkScenarioRegistryTests
{
    [Fact]
    public void Registry_ContainsAllRequiredScenarios()
    {
        IReadOnlyList<BenchmarkScenario> scenarios = BenchmarkScenarioRegistry.GetAll();

        Assert.Equal(3, scenarios.Count);
        Assert.Contains(scenarios, s => s.Id == BenchmarkScenarioId.Database);
        Assert.Contains(scenarios, s => s.Id == BenchmarkScenarioId.Collections);
        Assert.Contains(scenarios, s => s.Id == BenchmarkScenarioId.Json);
    }

    [Fact]
    public void Registry_ScenariosContainLearningObjectives()
    {
        IReadOnlyList<BenchmarkScenario> scenarios = BenchmarkScenarioRegistry.GetAll();

        Assert.All(scenarios, scenario => Assert.NotEmpty(scenario.LearningObjectives));
    }

    [Fact]
    public void Registry_AllScenariosAreImplemented()
    {
        IReadOnlyList<BenchmarkScenario> scenarios = BenchmarkScenarioRegistry.GetAll();

        Assert.All(scenarios, scenario => Assert.True(scenario.IsImplemented));
    }
}
