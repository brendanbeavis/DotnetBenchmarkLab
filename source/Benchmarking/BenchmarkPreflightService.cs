using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Data.Database;
using DotNetPerformanceLab.Data.Json;
using DotNetPerformanceLab.Data.AsyncIo;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class BenchmarkPreflightService : IBenchmarkPreflightService
{
    private readonly IReadOnlyDictionary<BenchmarkScenarioId, IScenarioCorrectnessValidator> _validators;
    private readonly Func<IReadOnlyList<BenchmarkScenario>> _scenarioProvider;

    public BenchmarkPreflightService(
        IEnumerable<IScenarioCorrectnessValidator>? validators = null,
        Func<IReadOnlyList<BenchmarkScenario>>? scenarioProvider = null)
    {
        _validators = (validators ?? CreateDefaultValidators())
            .ToDictionary(validator => validator.ScenarioId);
        _scenarioProvider = scenarioProvider ?? BenchmarkScenarioRegistry.GetAll;
    }

    public BenchmarkPreflightResult Validate(BenchmarkRunOptions options)
    {
        IReadOnlyList<BenchmarkScenario> availableScenarios = _scenarioProvider();
        IReadOnlyList<BenchmarkScenario> scenariosToValidate = options.RunAllScenarios
            ? availableScenarios
            : options.ScenarioId is { } scenarioId
                ? [availableScenarios.First(s => s.Id == scenarioId)]
                : [];

        List<ScenarioPreflightResult> results = new(scenariosToValidate.Count);

        foreach (BenchmarkScenario scenario in scenariosToValidate)
        {
            int approxRecords = scenario.Id switch
            {
                BenchmarkScenarioId.Database => RetailDatasetProfiles.Get(options.DatasetSize).OrderCount,
                BenchmarkScenarioId.Collections => CollectionDatasetProfiles.Get(options.DatasetSize).TransactionCount,
                BenchmarkScenarioId.Json => JsonDatasetProfiles.Get(options.DatasetSize).OrderCount,
                BenchmarkScenarioId.AsyncIo => AsyncIoDatasetProfiles.Get(options.DatasetSize).RequestCount,
                _ => BenchmarkConventions.GetApproximateRecordCount(options.DatasetSize)
            };

            if (!scenario.IsImplemented)
            {
                results.Add(new ScenarioPreflightResult(
                    scenario.Id,
                    scenario.DisplayName,
                    approxRecords,
                    ScenarioPreflightStatus.Skipped,
                    $"Skipped: {scenario.StatusNote}",
                    []));
                continue;
            }

            if (!_validators.TryGetValue(scenario.Id, out IScenarioCorrectnessValidator? validator))
            {
                results.Add(new ScenarioPreflightResult(
                    scenario.Id,
                    scenario.DisplayName,
                    approxRecords,
                    ScenarioPreflightStatus.Failed,
                    "No correctness validator is registered for this scenario.",
                    []));
                continue;
            }

            try
            {
                results.Add(validator.Validate(options.DatasetSize, options.JsonGroup));
            }
            catch (Exception ex)
            {
                results.Add(new ScenarioPreflightResult(
                    scenario.Id,
                    scenario.DisplayName,
                    approxRecords,
                    ScenarioPreflightStatus.Failed,
                    $"Validation failed: {ex.Message}",
                    []));
            }
        }

        return new BenchmarkPreflightResult(options.DatasetSize, results);
    }

    private static IReadOnlyList<IScenarioCorrectnessValidator> CreateDefaultValidators() =>
    [
        new DatabaseScenarioCorrectnessValidator(),
        new CollectionsScenarioCorrectnessValidator(),
        new JsonScenarioCorrectnessValidator(),
        new AsyncIoScenarioCorrectnessValidator()
    ];
}
