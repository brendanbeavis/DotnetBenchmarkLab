using System.Diagnostics;
using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Data.Database;
using DotNetPerformanceLab.Data.Json;
using Spectre.Console;

namespace DotNetPerformanceLab.Application;

public sealed class App
{
    private readonly BenchmarkRunnerService _benchmarkRunnerService;

    public App(BenchmarkRunnerService benchmarkRunnerService)
    {
        _benchmarkRunnerService = benchmarkRunnerService;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                AnsiConsole.Clear();
                RenderHeader();

                string selectedAction = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select an action[/]")
                        .AddChoices(
                            "Run a benchmark scenario",
                            "Run all benchmark scenarios",
                            "Learn about benchmarking",
                            "View report location / recent report files",
                            "Exit"));

                switch (selectedAction)
                {
                    case "Run a benchmark scenario":
                        await HandleRunSingleScenarioAsync(cancellationToken);
                        break;
                    case "Run all benchmark scenarios":
                        await HandleRunAllScenariosAsync(cancellationToken);
                        break;
                    case "Learn about benchmarking":
                        ShowBenchmarkingGuidance();
                        break;
                    case "View report location / recent report files":
                        ShowReports();
                        break;
                    case "Exit":
                        return 0;
                }
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return 1;
        }
    }

    private static void RenderHeader()
    {
        AnsiConsole.Write(new Rule("[bold cyan]DotNetPerformanceLab[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine("[grey]Internal developer education project for practical .NET performance benchmarking.[/]");
        AnsiConsole.MarkupLine("[grey]Authoritative measurements are produced by BenchmarkDotNet only.[/]\n");
    }

    private async Task HandleRunSingleScenarioAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<BenchmarkScenario> scenarios = BenchmarkScenarioRegistry.GetAll();
        BenchmarkScenario selectedScenario = AnsiConsole.Prompt(
            new SelectionPrompt<BenchmarkScenario>()
                .Title("[green]Choose a scenario[/]")
                .UseConverter(s => $"{s.DisplayName} ({(s.IsImplemented ? "Ready" : "Coming soon")})")
                .AddChoices(scenarios));

        ShowScenarioDetails(selectedScenario);

        DatasetSize datasetSize = PromptDatasetSize();

        ShowRunSummary(selectedScenario, datasetSize, runAll: false);

        if (!AnsiConsole.Confirm("Proceed?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[yellow]Run cancelled.[/]");
            WaitForMenuReturn();
            return;
        }

        if (!selectedScenario.IsImplemented)
        {
            AnsiConsole.MarkupLine($"[yellow]{selectedScenario.StatusNote}[/]");
            WaitForMenuReturn();
            return;
        }

        if (selectedScenario.Id == BenchmarkScenarioId.Json)
        {
            JsonBenchmarkGroup jsonGroup = PromptJsonGroup();
            await LaunchBenchmarkProcessAsync($"--benchmark json --dataset {datasetSize.ToString().ToLowerInvariant()} --json-group {ToCliJsonGroup(jsonGroup)}", cancellationToken);
            return;
        }

        await LaunchBenchmarkProcessAsync($"--benchmark {selectedScenario.Id.ToString().ToLowerInvariant()} --dataset {datasetSize.ToString().ToLowerInvariant()}", cancellationToken);
    }

    private async Task HandleRunAllScenariosAsync(CancellationToken cancellationToken)
    {
        DatasetSize datasetSize = PromptDatasetSize();
        BenchmarkScenario allScenarioSummary = new(
            BenchmarkScenarioId.Database,
            "All benchmark scenarios",
            "Run every registered scenario benchmark type.",
            [],
            IsImplemented: BenchmarkScenarioRegistry.GetAll().All(s => s.IsImplemented),
            StatusNote: "One or more scenarios are currently marked as coming soon.");

        ShowRunSummary(allScenarioSummary, datasetSize, runAll: true);

        if (!AnsiConsole.Confirm("Proceed?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[yellow]Run cancelled.[/]");
            WaitForMenuReturn();
            return;
        }

        if (!allScenarioSummary.IsImplemented)
        {
            AnsiConsole.MarkupLine($"[yellow]{allScenarioSummary.StatusNote}[/]");
            WaitForMenuReturn();
            return;
        }

        await LaunchBenchmarkProcessAsync($"--benchmark all --dataset {datasetSize.ToString().ToLowerInvariant()}", cancellationToken);
    }

    private static void ShowScenarioDetails(BenchmarkScenario scenario)
    {
        if (scenario.Id == BenchmarkScenarioId.Database)
        {
            Grid databaseGrid = new();
            databaseGrid.AddColumn();
            databaseGrid.AddRow("[bold]Business problem:[/] date-filtered retail order reporting with customer, line-item and product/category context.");
            databaseGrid.AddRow("[bold]Basic:[/] EF Core tracking.");
            databaseGrid.AddRow("[bold]Intermediate:[/] EF Core no-tracking, EF Core DTO projection.");
            databaseGrid.AddRow("[bold]Advanced:[/] EF Core compiled projection query, Dapper, raw ADO.NET.");
            AnsiConsole.Write(new Panel(databaseGrid).Header("Database scenario").Border(BoxBorder.Rounded));
            return;
        }

        if (scenario.Id == BenchmarkScenarioId.Collections)
        {
            Grid collectionsGrid = new();
            collectionsGrid.AddColumn();
            collectionsGrid.AddRow("[bold]Business problem:[/] aggregate retail transactions into totals, monetary metrics, and category groupings.");
            collectionsGrid.AddRow("[bold]Output:[/] transaction count, units sold, gross/discount/net sales, category sales and category counts.");
            collectionsGrid.AddRow("[bold]Basic:[/] LINQ aggregation.");
            collectionsGrid.AddRow("[bold]Intermediate:[/] foreach loop, dictionary aggregation.");
            collectionsGrid.AddRow("[bold]Advanced:[/] Parallel.For with thread-local state, PLINQ aggregate.");
            collectionsGrid.AddRow("[yellow]Large datasets with parallel variants may take longer depending on CPU and memory pressure.[/]");
            AnsiConsole.Write(new Panel(collectionsGrid).Header("Collections scenario").Border(BoxBorder.Rounded));
            return;
        }

        if (scenario.Id == BenchmarkScenarioId.Json)
        {
            Grid jsonGrid = new();
            jsonGrid.AddColumn();
            jsonGrid.AddRow("[bold]Business problem:[/] parse retail order API payloads and transform into application response summaries.");
            jsonGrid.AddRow("[bold]Groups:[/] parser-only and end-to-end processing.");
            jsonGrid.AddRow("[bold]Basic:[/] Newtonsoft.Json typed deserialization.");
            jsonGrid.AddRow("[bold]Intermediate:[/] System.Text.Json standard typed deserialization.");
            jsonGrid.AddRow("[bold]Advanced:[/] System.Text.Json source generation and streaming order processing.");
            jsonGrid.AddRow("[yellow]Large payloads can significantly increase memory pressure and run time.[/]");
            AnsiConsole.Write(new Panel(jsonGrid).Header("JSON scenario").Border(BoxBorder.Rounded));
        }
    }

    private static DatasetSize PromptDatasetSize()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<DatasetSize>()
                .Title("[green]Choose dataset size[/]")
                .AddChoices(DatasetSize.Small, DatasetSize.Medium, DatasetSize.Large));
    }

    private static JsonBenchmarkGroup PromptJsonGroup()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<JsonBenchmarkGroup>()
                .Title("[green]Choose JSON benchmark group[/]")
                .UseConverter(group => group switch
                {
                    JsonBenchmarkGroup.ParserOnly => "Parser-only",
                    JsonBenchmarkGroup.EndToEnd => "End-to-end",
                    _ => "All"
                })
                .AddChoices(JsonBenchmarkGroup.All, JsonBenchmarkGroup.ParserOnly, JsonBenchmarkGroup.EndToEnd));
    }

    private static string ToCliJsonGroup(JsonBenchmarkGroup group) => group switch
    {
        JsonBenchmarkGroup.All => "all",
        JsonBenchmarkGroup.ParserOnly => "parser",
        JsonBenchmarkGroup.EndToEnd => "endtoend",
        _ => throw new ArgumentOutOfRangeException(nameof(group), group, "Unsupported JSON benchmark group.")
    };

    private static void ShowRunSummary(BenchmarkScenario scenario, DatasetSize datasetSize, bool runAll)
    {
        Table table = new();
        table.Border = TableBorder.Rounded;
        table.AddColumn("Setting");
        table.AddColumn("Value");
        table.AddRow("Run type", runAll ? "All scenarios" : "Single scenario");
        table.AddRow("Scenario", scenario.DisplayName);
        table.AddRow("Dataset", datasetSize.ToString());

        int approxRecords = scenario.Id switch
        {
            BenchmarkScenarioId.Database => RetailDatasetProfiles.Get(datasetSize).OrderCount,
            BenchmarkScenarioId.Collections => CollectionDatasetProfiles.Get(datasetSize).TransactionCount,
            BenchmarkScenarioId.Json => JsonDatasetProfiles.Get(datasetSize).OrderCount,
            _ => BenchmarkConventions.GetApproximateRecordCount(datasetSize)
        };

        table.AddRow("Approx records", approxRecords.ToString("N0"));

        AnsiConsole.Write(table);

        string runtimeWarning = datasetSize switch
        {
            DatasetSize.Small => "Small workload; suitable for quick smoke checks.",
            DatasetSize.Medium => "Medium workload; run time depends on scenario and machine load.",
            DatasetSize.Large => "Large workload; may take several minutes and consume significant memory.",
            _ => "Benchmark run time depends on scenario and environment."
        };

        AnsiConsole.MarkupLine($"[yellow]{runtimeWarning}[/]");
        AnsiConsole.MarkupLine("[yellow]Run in Release mode outside the debugger for representative measurements.[/]\n");
    }

    private async Task LaunchBenchmarkProcessAsync(string benchmarkArgs, CancellationToken cancellationToken)
    {
        string assemblyPath = BenchmarkConventions.GetExecutablePath();

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"\"{assemblyPath}\" {benchmarkArgs}",
            UseShellExecute = false
        };

        AnsiConsole.MarkupLine("[green]Launching isolated benchmark process...[/]");

        using Process? process = Process.Start(startInfo);
        if (process is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to start benchmark process.[/]");
            WaitForMenuReturn();
            return;
        }

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            AnsiConsole.MarkupLine("[yellow]Benchmark run cancelled. Child benchmark process was terminated.[/]");
            WaitForMenuReturn();
            return;
        }

        AnsiConsole.MarkupLine($"[grey]Benchmark process exited with code {process.ExitCode}.[/]");

        if (process.ExitCode == 0)
        {
            ShowLatestReportsAfterRun();
        }

        WaitForMenuReturn();
    }

    private void ShowLatestReportsAfterRun()
    {
        ShowReportDirectoryHints();

        IReadOnlyList<string> recentReports = _benchmarkRunnerService.GetRecentReportFiles();
        if (recentReports.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No report files found for this run yet.[/]");
            return;
        }

        Table table = new();
        table.Border = TableBorder.Minimal;
        table.AddColumn("Report file");
        table.AddColumn("Last write (UTC)");

        foreach (string report in recentReports)
        {
            DateTime lastWriteUtc = File.GetLastWriteTimeUtc(report);
            table.AddRow(report, lastWriteUtc.ToString("u"));
        }

        AnsiConsole.MarkupLine("[grey]If these files pre-date this execution, treat them as previous run artifacts.[/]");
        AnsiConsole.Write(table);
    }

    private static void ShowBenchmarkingGuidance()
    {
        string content =
            "[bold]How to read results[/]\n" +
            "- Mean: average execution time per operation (lower is generally better).\n" +
            "- Error/StdDev: stability and variance indicators; lower variance improves confidence.\n" +
            "- Throughput (ops/sec): higher is better when reported.\n" +
            "- Allocated: managed bytes per operation (lower often reduces GC pressure).\n" +
            "- Gen0/Gen1/Gen2: garbage collections per 1000 operations.\n" +
            "- Ratio: relative to baseline (ratio < 1 means faster/lower allocation for those metrics).\n\n" +
            "[bold]Interpretation caveats[/]\n" +
            "- Faster microbenchmark results are not automatically better engineering outcomes.\n" +
            "- Results vary by CPU, memory, OS, runtime, power plan, and background load.\n" +
            "- Repeat runs on comparable environments before drawing conclusions.\n" +
            "- Microbenchmarks do not directly predict end-to-end production latency.\n" +
            "- BenchmarkDotNet controls warmup and measurement; avoid stopwatch-only claims.";

        AnsiConsole.Write(new Panel(content).Header("Benchmark interpretation").Border(BoxBorder.Rounded));
        WaitForMenuReturn();
    }

    private void ShowReports()
    {
        ShowReportDirectoryHints();
        IReadOnlyList<string> recentReports = _benchmarkRunnerService.GetRecentReportFiles();

        if (recentReports.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No reports found yet.[/]");
            WaitForMenuReturn();
            return;
        }

        Table table = new();
        table.Border = TableBorder.Minimal;
        table.AddColumn("Previous run report file");
        table.AddColumn("Last write (UTC)");

        foreach (string report in recentReports)
        {
            table.AddRow(report, File.GetLastWriteTimeUtc(report).ToString("u"));
        }

        AnsiConsole.Write(table);
        WaitForMenuReturn();
    }

    private static void ShowReportDirectoryHints()
    {
        IReadOnlyList<string> candidates = BenchmarkConventions.GetCandidateReportsDirectories();
        AnsiConsole.MarkupLine("[green]Report directories checked:[/]");
        foreach (string directory in candidates)
        {
            AnsiConsole.MarkupLine($"  - {directory}");
        }
    }

    private static void WaitForMenuReturn()
    {
        AnsiConsole.MarkupLine("\n[grey]Press any key to return to the main menu...[/]");
        Console.ReadKey(intercept: true);
    }
}
