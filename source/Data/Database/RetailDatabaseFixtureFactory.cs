using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Generators;
using DotNetPerformanceLab.Reporting.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceLab.Data.Database;

public static class RetailDatabaseFixtureFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "DotNetPerformanceLab", "sqlite-database-scenario");

    public static RetailDatabaseFixture Create(DatasetSize datasetSize)
    {
        return Create(datasetSize, seedAction: null);
    }

    internal static RetailDatabaseFixture Create(
        DatasetSize datasetSize,
        Action<RetailDbContext, RetailDatasetProfile, int>? seedAction)
    {
        RetailDatasetProfile profile = RetailDatasetProfiles.Get(datasetSize);
        Directory.CreateDirectory(RootDirectory);

        string databasePath = Path.Combine(
            RootDirectory,
            $"retail-{datasetSize.ToString().ToLowerInvariant()}-{Environment.ProcessId}-{Guid.NewGuid():N}.db");

        string connectionString = RetailDatabaseFixture.BuildConnectionString(databasePath, SqliteOpenMode.ReadWriteCreate, pooling: true);

        DbContextOptions<RetailDbContext> options = new DbContextOptionsBuilder<RetailDbContext>()
            .UseSqlite(connectionString)
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false)
            .Options;

        try
        {
            using RetailDbContext setupContext = new(options);
            setupContext.Database.EnsureDeleted();
            setupContext.Database.EnsureCreated();
            setupContext.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
            setupContext.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");
            setupContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");

            (seedAction ?? RetailDatabaseSeeder.Seed)(setupContext, profile, BenchmarkConventions.DeterministicSeed);

            DatabaseReportQuery reportQuery = new(
                profile.BaseOrderDateUtc.AddDays(profile.QueryWindowStartDayOffset),
                profile.BaseOrderDateUtc.AddDays(profile.QueryWindowStartDayOffset + profile.QueryWindowLengthDays));

            return new RetailDatabaseFixture(datasetSize, databasePath, RootDirectory, options, reportQuery);
        }
        catch
        {
            RetailDatabaseFixture.CleanupOwnedFiles(databasePath, RootDirectory, connectionString, throwOnDeleteError: false);
            throw;
        }
    }
}
