using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Reporting.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace DotNetPerformanceLab.Data.Database;

public sealed class RetailDatabaseFixture
{
    public RetailDatabaseFixture(DatasetSize datasetSize, string databasePath, DbContextOptions<RetailDbContext> dbContextOptions, DatabaseReportQuery reportQuery)
    {
        DatasetSize = datasetSize;
        DatabasePath = databasePath;
        DbContextOptions = dbContextOptions;
        ReportQuery = reportQuery;
    }

    public DatasetSize DatasetSize { get; }

    public string DatabasePath { get; }

    public DbContextOptions<RetailDbContext> DbContextOptions { get; }

    public DatabaseReportQuery ReportQuery { get; }

    public string ConnectionString => new SqliteConnectionStringBuilder
    {
        DataSource = DatabasePath,
        Mode = SqliteOpenMode.ReadWrite,
        Cache = SqliteCacheMode.Shared,
        Pooling = true
    }.ToString();

    public RetailDbContext CreateDbContext() => new(DbContextOptions);
}
