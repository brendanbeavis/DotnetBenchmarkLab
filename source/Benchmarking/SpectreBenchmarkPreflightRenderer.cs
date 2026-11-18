using Spectre.Console;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class SpectreBenchmarkPreflightRenderer : IBenchmarkPreflightRenderer
{
    public void Render(BenchmarkPreflightResult result)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Correctness preflight[/]").RuleStyle("grey"));

        foreach (ScenarioPreflightResult scenarioResult in result.ScenarioResults)
        {
            string statusMarkup = scenarioResult.Status switch
            {
                ScenarioPreflightStatus.Passed => "[green]PASSED[/]",
                ScenarioPreflightStatus.Failed => "[red]FAILED[/]",
                _ => "[yellow]SKIPPED[/]"
            };

            Table table = new();
            table.Border = TableBorder.Rounded;
            table.AddColumn("Scenario");
            table.AddColumn("Dataset");
            table.AddColumn("Approx records");
            table.AddColumn("Status");
            table.AddRow(
                scenarioResult.ScenarioName,
                result.DatasetSize.ToString(),
                scenarioResult.ApproximateRecordCount.ToString("N0"),
                statusMarkup);

            AnsiConsole.Write(table);

            if (scenarioResult.ImplementationsChecked.Count > 0)
            {
                Table implementationTable = new();
                implementationTable.Border = TableBorder.Minimal;
                implementationTable.AddColumn("Implementation");
                implementationTable.AddColumn("Status");
                implementationTable.AddColumn("Detail");

                foreach (ImplementationPreflightResult implementation in scenarioResult.ImplementationsChecked)
                {
                    implementationTable.AddRow(
                        implementation.Name,
                        implementation.Passed ? "[green]Pass[/]" : "[red]Fail[/]",
                        implementation.Detail);
                }

                AnsiConsole.Write(implementationTable);
            }

            if (!string.IsNullOrWhiteSpace(scenarioResult.Message))
            {
                string messageStyle = scenarioResult.Status == ScenarioPreflightStatus.Failed ? "red" : "grey";
                AnsiConsole.MarkupLine($"[{messageStyle}]{scenarioResult.Message.EscapeMarkup()}[/]");
            }

            AnsiConsole.WriteLine();
        }

        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine("[green]Correctness preflight passed: all compared implementations returned equivalent results.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Correctness preflight failed. Benchmark execution has been stopped.[/]");
        }
    }
}
