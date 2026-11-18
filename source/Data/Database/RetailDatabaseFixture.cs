using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Reporting.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceLab.Data.Database;

public sealed class RetailDatabaseFixture : IDisposable
{
    private readonly string _ownedRootDirectory;
    private readonly string _fullDatabasePath;
    private int _disposeSignaled;

    public RetailDatabaseFixture(
        DatasetSize datasetSize,
        string databasePath,
        string ownedRootDirectory,
        DbContextOptions<RetailDbContext> dbContextOptions,
        DatabaseReportQuery reportQuery)
    {
        DatasetSize = datasetSize;
        _fullDatabasePath = Path.GetFullPath(databasePath);
        _ownedRootDirectory = EnsureTrailingDirectorySeparator(Path.GetFullPath(ownedRootDirectory));
        ValidateOwnedPath(_fullDatabasePath, _ownedRootDirectory);

        DatabasePath = _fullDatabasePath;
        DbContextOptions = dbContextOptions;
        ReportQuery = reportQuery;

        ConnectionString = BuildConnectionString(DatabasePath, SqliteOpenMode.ReadWrite, pooling: true);
    }

    public DatasetSize DatasetSize { get; }

    public string DatabasePath { get; }

    public DbContextOptions<RetailDbContext> DbContextOptions { get; }

    public DatabaseReportQuery ReportQuery { get; }

    public string ConnectionString { get; }

    public RetailDbContext CreateDbContext() => new(DbContextOptions);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeSignaled, 1) != 0)
        {
            return;
        }

        CleanupOwnedFiles(DatabasePath, _ownedRootDirectory, ConnectionString, throwOnDeleteError: true);
        GC.SuppressFinalize(this);
    }

    internal static void CleanupOwnedFiles(
        string databasePath,
        string ownedRootDirectory,
        string connectionString,
        bool throwOnDeleteError)
    {
        string fullDatabasePath = Path.GetFullPath(databasePath);
        string fullOwnedRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(ownedRootDirectory));

        ValidateOwnedPath(fullDatabasePath, fullOwnedRoot);
        ClearSqlitePool(connectionString);

        DeleteFileIfExists(fullDatabasePath, throwOnDeleteError);
        DeleteFileIfExists(GetWalPath(fullDatabasePath), throwOnDeleteError);
        DeleteFileIfExists(GetShmPath(fullDatabasePath), throwOnDeleteError);
    }

    internal static string BuildConnectionString(string databasePath, SqliteOpenMode mode, bool pooling)
    {
        return new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = mode,
            Cache = SqliteCacheMode.Shared,
            Pooling = pooling
        }.ToString();
    }

    private static void ClearSqlitePool(string connectionString)
    {
        SqliteConnection.ClearAllPools();
        using SqliteConnection poolConnection = new(connectionString);
        SqliteConnection.ClearPool(poolConnection);
    }

    private static void DeleteFileIfExists(string path, bool throwOnDeleteError)
    {
        const int maxAttempts = 6;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }

                File.Delete(path);
                return;
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(25 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(25 * attempt);
            }
            catch when (!throwOnDeleteError)
            {
                return;
            }
        }

        if (throwOnDeleteError)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static string GetWalPath(string databasePath) => databasePath + "-wal";

    private static string GetShmPath(string databasePath) => databasePath + "-shm";

    private static void ValidateOwnedPath(string fullDatabasePath, string fullOwnedRoot)
    {
        if (!fullDatabasePath.StartsWith(fullOwnedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to clean up database path outside fixture temp root: '{fullDatabasePath}'.");
        }
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }
}
