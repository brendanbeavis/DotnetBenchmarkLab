namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkCommandLineParser
{
    public static bool TryParse(string[] args, out BenchmarkRunOptions options, out string? error)
    {
        options = BenchmarkRunOptions.InteractiveDefault;
        error = null;

        if (args.Length == 0)
        {
            return true;
        }

        if (args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)))
        {
            options = options with { IsHelpRequested = true };
            return true;
        }

        DatasetSize datasetSize = DatasetSize.Medium;
        JsonBenchmarkGroup jsonGroup = JsonBenchmarkGroup.All;
        bool skipPreflight = false;
        bool benchmarkMode = false;
        bool runAll = false;
        BenchmarkScenarioId? scenario = null;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];

            if (string.Equals(arg, "--skip-preflight", StringComparison.OrdinalIgnoreCase))
            {
                skipPreflight = true;
                continue;
            }

            if (string.Equals(arg, "--benchmark", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                {
                    error = "Missing benchmark target. Use --benchmark all|database|collections|json|asyncio.";
                    return false;
                }

                benchmarkMode = true;
                string benchmarkTarget = args[++index];

                if (string.Equals(benchmarkTarget, "all", StringComparison.OrdinalIgnoreCase))
                {
                    runAll = true;
                    scenario = null;
                    continue;
                }

                if (!TryParseScenario(benchmarkTarget, out BenchmarkScenarioId parsedScenario))
                {
                    error = $"Invalid benchmark target '{benchmarkTarget}'. Use all|database|collections|json|asyncio.";
                    return false;
                }

                runAll = false;
                scenario = parsedScenario;
                continue;
            }

            if (string.Equals(arg, "--dataset", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                {
                    error = "Missing dataset size. Use --dataset small|medium|large.";
                    return false;
                }

                string datasetToken = args[++index];
                if (!Enum.TryParse(datasetToken, ignoreCase: true, out datasetSize))
                {
                    error = $"Invalid dataset size '{datasetToken}'. Use small|medium|large.";
                    return false;
                }

                continue;
            }

            if (string.Equals(arg, "--json-group", StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Length)
                {
                    error = "Missing JSON group. Use --json-group all|parser|endtoend.";
                    return false;
                }

                string jsonGroupToken = args[++index];
                if (!TryParseJsonGroup(jsonGroupToken, out jsonGroup))
                {
                    error = $"Invalid JSON group '{jsonGroupToken}'. Use all|parser|endtoend.";
                    return false;
                }

                continue;
            }

            error = $"Unknown argument '{arg}'. Use --help for supported options.";
            return false;
        }

        if (!benchmarkMode)
        {
            error = "Arguments were provided but benchmark mode was not selected. Use --benchmark ... or run without args for interactive mode.";
            return false;
        }

        options = new BenchmarkRunOptions(
            IsBenchmarkMode: true,
            RunAllScenarios: runAll,
            ScenarioId: scenario,
            DatasetSize: datasetSize,
            JsonGroup: jsonGroup,
            IsHelpRequested: false,
            SkipPreflight: skipPreflight);

        return true;
    }

    private static bool TryParseScenario(string token, out BenchmarkScenarioId scenario)
    {
        scenario = token.ToLowerInvariant() switch
        {
            "database" => BenchmarkScenarioId.Database,
            "collections" => BenchmarkScenarioId.Collections,
            "json" => BenchmarkScenarioId.Json,
            "asyncio" => BenchmarkScenarioId.AsyncIo,
            _ => default
        };

        return token.Equals("database", StringComparison.OrdinalIgnoreCase)
            || token.Equals("collections", StringComparison.OrdinalIgnoreCase)
            || token.Equals("json", StringComparison.OrdinalIgnoreCase)
            || token.Equals("asyncio", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseJsonGroup(string token, out JsonBenchmarkGroup group)
    {
        group = token.ToLowerInvariant() switch
        {
            "all" => JsonBenchmarkGroup.All,
            "parser" => JsonBenchmarkGroup.ParserOnly,
            "endtoend" => JsonBenchmarkGroup.EndToEnd,
            _ => default
        };

        return token.Equals("all", StringComparison.OrdinalIgnoreCase)
            || token.Equals("parser", StringComparison.OrdinalIgnoreCase)
            || token.Equals("endtoend", StringComparison.OrdinalIgnoreCase);
    }
}
