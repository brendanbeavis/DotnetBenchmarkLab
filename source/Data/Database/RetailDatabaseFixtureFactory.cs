using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Reporting.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceLab.Data.Database;

public static class RetailDatabaseFixtureFactory
{
    public static RetailDatabaseFixture Create(DatasetSize datasetSize)
    {
        RetailDatasetProfile profile = RetailDatasetProfiles.Get(datasetSize);
        string rootDirectory = Path.Combine(Path.GetTempPath(), "DotNetPerformanceLab", "sqlite-database-scenario");
        Directory.CreateDirectory(rootDirectory);

        string databasePath = Path.Combine(
            rootDirectory,
            $"retail-{datasetSize.ToString().ToLowerInvariant()}-{Environment.ProcessId}-{Guid.NewGuid():N}.db");

        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();

        DbContextOptions<RetailDbContext> options = new DbContextOptionsBuilder<RetailDbContext>()
            .UseSqlite(connectionString)
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false)
            .Options;

        using (RetailDbContext setupContext = new(options))
        {
            setupContext.Database.EnsureDeleted();
            setupContext.Database.EnsureCreated();
            setupContext.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
            setupContext.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");
            setupContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");

            RetailDatabaseSeeder.Seed(setupContext, profile, BenchmarkConventions.DeterministicSeed);
        }

        DatabaseReportQuery reportQuery = new(
            profile.BaseOrderDateUtc.AddDays(profile.QueryWindowStartDayOffset),
            profile.BaseOrderDateUtc.AddDays(profile.QueryWindowStartDayOffset + profile.QueryWindowLengthDays));

        return new RetailDatabaseFixture(datasetSize, databasePath, options, reportQuery);
    }
}
