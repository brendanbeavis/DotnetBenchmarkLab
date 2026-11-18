namespace DotNetPerformanceLab.Benchmarking;

public interface IPerformanceLabSummaryRenderer
{
    void Render(PerformanceLabSummaryRun summary);
}
