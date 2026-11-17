namespace DotNetPerformanceLab.Reporting.Database;

public sealed record DatabaseReportQuery(DateTime StartDateUtcInclusive, DateTime EndDateUtcExclusive);
