using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

namespace DotNetPerformanceLab.Benchmarking;

public static class BenchmarkConfigFactory
{
    public static IConfig Create()
    {
        ManualConfig config = ManualConfig.Create(DefaultConfig.Instance);

        if (config.GetExporters().All(exporter => exporter.Name != HtmlExporter.Default.Name))
        {
            config.AddExporter(HtmlExporter.Default);
        }

        if (config.GetExporters().All(exporter => exporter.Name != MarkdownExporter.GitHub.Name))
        {
            config.AddExporter(MarkdownExporter.GitHub);
        }

        if (config.GetExporters().All(exporter => exporter.Name != CsvExporter.Default.Name))
        {
            config.AddExporter(CsvExporter.Default);
        }

        return config;
    }
}
