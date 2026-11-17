namespace DotNetPerformanceLab.Data.Database;

public enum DatabaseReportingApproach
{
    EfTracking,
    EfNoTracking,
    EfProjection,
    EfCompiledProjection,
    Dapper,
    AdoNet
}
