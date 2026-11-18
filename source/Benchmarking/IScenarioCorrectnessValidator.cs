namespace DotNetPerformanceLab.Benchmarking;

public interface IScenarioCorrectnessValidator
{
    BenchmarkScenarioId ScenarioId { get; }

    ScenarioPreflightResult Validate(DatasetSize datasetSize, JsonBenchmarkGroup jsonGroup);
}
