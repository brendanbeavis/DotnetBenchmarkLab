using DotNetPerformanceLab.Application;
using DotNetPerformanceLab.Benchmarking;
using Spectre.Console;

if (!BenchmarkCommandLineParser.TryParse(args, out BenchmarkRunOptions options, out string? parseError))
{
    AnsiConsole.MarkupLine($"[red]{parseError}[/]");
    PrintUsage();
    return 1;
}

if (options.IsHelpRequested)
{
    PrintUsage();
    return 0;
}

BenchmarkRunnerService benchmarkRunnerService = new();

if (options.IsBenchmarkMode)
{
    return benchmarkRunnerService.Run(options);
}

App app = new(benchmarkRunnerService);
return await app.RunAsync();

static void PrintUsage()
{
    AnsiConsole.MarkupLine("[green]Usage[/]");
    AnsiConsole.MarkupLine("  dotnet run -c Release -- --benchmark all --dataset medium");
    AnsiConsole.MarkupLine("  dotnet run -c Release -- --benchmark database --dataset small");
    AnsiConsole.MarkupLine("  dotnet run -c Release -- --benchmark collections --dataset medium");
    AnsiConsole.MarkupLine("  dotnet run -c Release -- --benchmark asyncio --dataset medium");
    AnsiConsole.MarkupLine("  dotnet run -c Release -- --benchmark json --dataset large --json-group all");
    AnsiConsole.MarkupLine("  dotnet run -c Release -- --benchmark json --dataset small --json-group parser");
    AnsiConsole.MarkupLine("  dotnet run -c Release -- --benchmark json --dataset small --json-group endtoend");
    AnsiConsole.MarkupLine("  dotnet run -- (interactive menu)\n");
}
