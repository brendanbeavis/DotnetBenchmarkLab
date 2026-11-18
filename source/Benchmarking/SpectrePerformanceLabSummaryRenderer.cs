using Spectre.Console;

namespace DotNetPerformanceLab.Benchmarking;

public sealed class SpectrePerformanceLabSummaryRenderer : IPerformanceLabSummaryRenderer
{
    public void Render(PerformanceLabSummaryRun summary)
    {
        AnsiConsole.Write(new Rule("[bold cyan]Performance Lab Summary[/]").RuleStyle("grey"));

        Grid header = new();
        header.AddColumn();
        header.AddRow($"[bold]Run ID:[/] {summary.RunId}");
        header.AddRow($"[bold]Generated (UTC):[/] {summary.GeneratedAtUtc:u}");
        header.AddRow($"[bold]Run:[/] {summary.RunLabel}");
        header.AddRow($"[bold]Dataset:[/] {summary.DatasetSize}");
        header.AddRow($"[bold]Correctness preflight:[/] {summary.PreflightStatus} - {summary.PreflightMessage.EscapeMarkup()}");
        header.AddRow("[grey]BenchmarkDotNet native reports remain authoritative for raw measurement diagnostics.[/]");
        header.AddRow("[grey]Allocated bytes/op represents managed allocations per operation, not peak process memory.[/]");
        AnsiConsole.Write(new Panel(header).Header("Summary metadata").Border(BoxBorder.Rounded));

        foreach (PerformanceLabSummaryGroup group in summary.Groups)
        {
            AnsiConsole.Write(new Rule($"[bold]{group.Scenario} | {group.BenchmarkGroup} | {group.BenchmarkType}[/]"));
            AnsiConsole.MarkupLine($"[grey]Environment:[/] {group.Environment.EscapeMarkup()}");

            if (group.HasMissingData)
            {
                AnsiConsole.MarkupLine($"[yellow]Summary completeness warning:[/] {group.MissingDataMessage.EscapeMarkup()}");
            }

            RenderTimeComparison(group.Entries);
            RenderAllocationComparison(group.Entries);
            RenderRelativeTable(group.Entries);
            RenderTradeoffGuidance(group.Entries);

            AnsiConsole.MarkupLine($"[bold]Interpretation:[/] {group.Interpretation.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[grey]What this does not prove: production-wide behavior across all datasets, hardware, and workloads.[/]");
            AnsiConsole.WriteLine();
        }
    }

    private static void RenderTimeComparison(IReadOnlyList<PerformanceLabSummaryEntry> entries)
    {
        Table table = new();
        table.Border = TableBorder.Minimal;
        table.AddColumn("Method");
        table.AddColumn("Level");
        table.AddColumn("Mean");
        table.AddColumn("Error");
        table.AddColumn("StdDev");
        table.AddColumn("Time bar");

        double max = entries.Where(x => x.MeanNanoseconds.HasValue).Select(x => x.MeanNanoseconds!.Value).DefaultIfEmpty(0d).Max();

        foreach (PerformanceLabSummaryEntry entry in entries)
        {
            string meanText = FormatDuration(entry.MeanNanoseconds);
            string errorText = FormatDuration(entry.ErrorNanoseconds);
            string stdDevText = FormatDuration(entry.StdDevNanoseconds);
            string bar = BuildBar(entry.MeanNanoseconds, max);

            table.AddRow(
                entry.IsBaseline ? $"[bold]{entry.Method} (baseline)[/]" : entry.Method,
                entry.ImplementationLevel,
                meanText,
                errorText,
                stdDevText,
                bar);
        }

        AnsiConsole.MarkupLine("[bold]Mean execution time comparison[/]");
        AnsiConsole.Write(table);
    }

    private static void RenderAllocationComparison(IReadOnlyList<PerformanceLabSummaryEntry> entries)
    {
        Table table = new();
        table.Border = TableBorder.Minimal;
        table.AddColumn("Method");
        table.AddColumn("Allocated bytes/op");
        table.AddColumn("Allocation bar");

        double max = entries.Where(x => x.AllocatedBytes.HasValue).Select(x => x.AllocatedBytes!.Value).DefaultIfEmpty(0d).Max();

        foreach (PerformanceLabSummaryEntry entry in entries)
        {
            string bytesText = entry.AllocatedBytes.HasValue ? $"{entry.AllocatedBytes.Value:N0} B" : "n/a";
            string bar = BuildBar(entry.AllocatedBytes, max);
            table.AddRow(
                entry.IsBaseline ? $"[bold]{entry.Method} (baseline)[/]" : entry.Method,
                bytesText,
                bar);
        }

        AnsiConsole.MarkupLine("[bold]Allocated bytes per operation comparison[/]");
        AnsiConsole.Write(table);
    }

    private static void RenderRelativeTable(IReadOnlyList<PerformanceLabSummaryEntry> entries)
    {
        Table table = new();
        table.Border = TableBorder.Minimal;
        table.AddColumn("Method");
        table.AddColumn("Time vs baseline");
        table.AddColumn("Allocation vs baseline");
        table.AddColumn("Clarity");

        foreach (PerformanceLabSummaryEntry entry in entries)
        {
            string time = entry.RelativeTimeRatio.HasValue
                ? entry.RelativeTimeRatio.Value <= 1
                    ? $"{1d / entry.RelativeTimeRatio.Value:0.##}x faster"
                    : $"{entry.RelativeTimeRatio.Value:0.##}x slower"
                : "n/a";

            string allocation = entry.RelativeAllocationRatio.HasValue
                ? entry.RelativeAllocationRatio.Value <= 1
                    ? $"{(1d - entry.RelativeAllocationRatio.Value) * 100d:0.#}% less"
                    : $"{(entry.RelativeAllocationRatio.Value - 1d) * 100d:0.#}% more"
                : "n/a";

            string clarity = entry.TimeClarity switch
            {
                ComparisonClarity.ClearDifference => "Clear",
                ComparisonClarity.NoClearDifference => "No clear difference measured",
                _ => "Unknown"
            };

            table.AddRow(entry.Method, time, allocation, clarity);
        }

        AnsiConsole.MarkupLine("[bold]Baseline-relative comparison[/]");
        AnsiConsole.Write(table);
    }

    private static void RenderTradeoffGuidance(IReadOnlyList<PerformanceLabSummaryEntry> entries)
    {
        Table table = new();
        table.Border = TableBorder.Minimal;
        table.AddColumn("Method");
        table.AddColumn("When to use");
        table.AddColumn("When not to use");

        bool hasRows = false;
        foreach (PerformanceLabSummaryEntry entry in entries)
        {
            if (!EngineeringTradeoffCatalog.TryGetByMethod(entry.Method, out EngineeringTradeoffInfo? info) || info is null)
            {
                continue;
            }

            hasRows = true;
            table.AddRow(entry.Method, info.WhenToUse, info.WhenNotToUse);
        }

        if (!hasRows)
        {
            return;
        }

        AnsiConsole.MarkupLine("[bold]Engineering trade-off guidance[/]");
        AnsiConsole.Write(table);
    }

    private static string BuildBar(double? value, double max)
    {
        if (!value.HasValue || max <= 0)
        {
            return "n/a";
        }

        const int maxWidth = 24;
        int width = (int)Math.Round((value.Value / max) * maxWidth, MidpointRounding.AwayFromZero);
        width = Math.Clamp(width, 1, maxWidth);
        return new string('█', width);
    }

    private static string FormatDuration(double? nanoseconds)
    {
        if (!nanoseconds.HasValue)
        {
            return "n/a";
        }

        double value = nanoseconds.Value;
        if (value >= 1_000_000_000d)
        {
            return $"{value / 1_000_000_000d:0.###} s";
        }

        if (value >= 1_000_000d)
        {
            return $"{value / 1_000_000d:0.###} ms";
        }

        if (value >= 1_000d)
        {
            return $"{value / 1_000d:0.###} µs";
        }

        return $"{value:0.###} ns";
    }
}
