using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Database;
using DotNetPerformanceLab.Benchmarks.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceLab.Tests;

public class DatabaseScenarioTests
{
    [Fact]
    public void Seeding_IsDeterministic_ForSmallDataset()
    {
        using RetailDatabaseFixture firstFixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small);
        using RetailDatabaseFixture secondFixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small);

        using RetailDbContext firstContext = firstFixture.CreateDbContext();
        using RetailDbContext secondContext = secondFixture.CreateDbContext();

        var firstSignature = firstContext.Orders
            .AsNoTracking()
            .OrderBy(o => o.Id)
            .Take(25)
            .Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.OrderDate,
                o.Status,
                LineCount = o.OrderItems.Count
            })
            .ToArray();

        var secondSignature = secondContext.Orders
            .AsNoTracking()
            .OrderBy(o => o.Id)
            .Take(25)
            .Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.OrderDate,
                o.Status,
                LineCount = o.OrderItems.Count
            })
            .ToArray();

        Assert.Equal(firstSignature.Length, secondSignature.Length);
        for (int index = 0; index < firstSignature.Length; index++)
        {
            Assert.Equal(firstSignature[index].Id, secondSignature[index].Id);
            Assert.Equal(firstSignature[index].CustomerId, secondSignature[index].CustomerId);
            Assert.Equal(firstSignature[index].OrderDate, secondSignature[index].OrderDate);
            Assert.Equal(firstSignature[index].Status, secondSignature[index].Status);
            Assert.Equal(firstSignature[index].LineCount, secondSignature[index].LineCount);
        }
    }

    [Fact]
    public void SeededData_HasReferentialIntegrity()
    {
        using RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small);
        using RetailDbContext dbContext = fixture.CreateDbContext();

        int orphanOrders = dbContext.Orders.Count(order => !dbContext.Customers.Any(customer => customer.Id == order.CustomerId));
        int orphanOrderItemsByOrder = dbContext.OrderItems.Count(item => !dbContext.Orders.Any(order => order.Id == item.OrderId));
        int orphanOrderItemsByProduct = dbContext.OrderItems.Count(item => !dbContext.Products.Any(product => product.Id == item.ProductId));

        Assert.Equal(0, orphanOrders);
        Assert.Equal(0, orphanOrderItemsByOrder);
        Assert.Equal(0, orphanOrderItemsByProduct);
    }

    [Fact]
    public void AllImplementations_ReturnEquivalentResults()
    {
        using RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small);
        DatabaseReportingQueryService queryService = new(fixture);
        DatabaseCorrectnessVerifier verifier = new(queryService);

        DatabaseCorrectnessVerificationResult verification = verifier.VerifyAllEquivalent(fixture.ReportQuery, decimalTolerance: 0.01m);

        Assert.Equal(6, verification.ResultsByApproach.Count);
    }

    [Fact]
    public void Report_IsFilteredAndOrdered()
    {
        using RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small);
        DatabaseReportingQueryService queryService = new(fixture);

        var report = queryService.RunEfProjection(fixture.ReportQuery);

        Assert.NotEmpty(report);
        Assert.All(report, order =>
        {
            Assert.True(order.OrderDate >= fixture.ReportQuery.StartDateUtcInclusive);
            Assert.True(order.OrderDate < fixture.ReportQuery.EndDateUtcExclusive);
        });

        for (int index = 1; index < report.Count; index++)
        {
            var previous = report[index - 1];
            var current = report[index];

            bool ordered = previous.OrderDate < current.OrderDate
                || (previous.OrderDate == current.OrderDate && previous.OrderId <= current.OrderId);

            Assert.True(ordered);
        }
    }

    [Theory]
    [InlineData(DatasetSize.Small, 1_000)]
    [InlineData(DatasetSize.Medium, 50_000)]
    [InlineData(DatasetSize.Large, 250_000)]
    public void DatasetProfiles_MapToExpectedOrderCounts(DatasetSize datasetSize, int expectedOrderCount)
    {
        RetailDatasetProfile profile = RetailDatasetProfiles.Get(datasetSize);
        Assert.Equal(expectedOrderCount, profile.OrderCount);
    }

    [Fact]
    public void FixtureCreation_CreatesDatabaseFile_AndDisposeRemovesOwnedFiles()
    {
        string dbPath;
        string walPath;
        string shmPath;

        using (RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small))
        {
            dbPath = fixture.DatabasePath;
            walPath = dbPath + "-wal";
            shmPath = dbPath + "-shm";

            Assert.True(File.Exists(dbPath));

            using RetailDbContext dbContext = fixture.CreateDbContext();
            _ = dbContext.Orders.AsNoTracking().Take(1).ToArray();
        }

        Assert.False(File.Exists(dbPath));
        Assert.False(File.Exists(walPath));
        Assert.False(File.Exists(shmPath));
    }

    [Fact]
    public void FixtureDispose_CanBeCalledTwice()
    {
        RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small);
        string dbPath = fixture.DatabasePath;

        fixture.Dispose();
        fixture.Dispose();

        Assert.False(File.Exists(dbPath));
        Assert.False(File.Exists(dbPath + "-wal"));
        Assert.False(File.Exists(dbPath + "-shm"));
    }

    [Fact]
    public void FixtureCreationFailure_CleansPartialDatabaseFiles()
    {
        string? capturedDbPath = null;

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            RetailDatabaseFixtureFactory.Create(
                DatasetSize.Small,
                (context, profile, seed) =>
                {
                    SqliteConnectionStringBuilder builder = new(context.Database.GetConnectionString());
                    capturedDbPath = builder.DataSource;
                    throw new InvalidOperationException("Seed failure for test.");
                }));

        Assert.Equal("Seed failure for test.", ex.Message);
        Assert.False(string.IsNullOrWhiteSpace(capturedDbPath));

        Assert.False(File.Exists(capturedDbPath!));
        Assert.False(File.Exists(capturedDbPath + "-wal"));
        Assert.False(File.Exists(capturedDbPath + "-shm"));
    }

    [Fact]
    public void DatabaseBenchmarks_GlobalCleanup_DisposesFixtureFiles()
    {
        DatabaseReportingBenchmarks benchmarks = new()
        {
            Dataset = nameof(DatasetSize.Small)
        };

        benchmarks.Setup();
        _ = benchmarks.EfCoreTracking();
        string fixturePath = GetBenchmarkFixturePath(benchmarks);
        Assert.True(File.Exists(fixturePath));

        benchmarks.Cleanup();

        Assert.False(File.Exists(fixturePath));
        Assert.False(File.Exists(fixturePath + "-wal"));
        Assert.False(File.Exists(fixturePath + "-shm"));
    }

    [Fact]
    public void FixtureDispose_ClearsPooledConnectionsAndRemovesFiles()
    {
        RetailDatabaseFixture fixture = RetailDatabaseFixtureFactory.Create(DatasetSize.Small);
        string fixturePath = fixture.DatabasePath;

        using (SqliteConnection pooledConnection = new(fixture.ConnectionString))
        {
            pooledConnection.Open();
            using SqliteCommand command = pooledConnection.CreateCommand();
            command.CommandText = "SELECT 1";
            _ = command.ExecuteScalar();
        }

        fixture.Dispose();

        Assert.False(File.Exists(fixturePath));
        Assert.False(File.Exists(fixturePath + "-wal"));
        Assert.False(File.Exists(fixturePath + "-shm"));
    }

    private static string GetBenchmarkFixturePath(object benchmark)
    {
        Type type = benchmark.GetType();
        var field = type.GetField("_fixture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);

        RetailDatabaseFixture? fixture = field.GetValue(benchmark) as RetailDatabaseFixture;
        Assert.NotNull(fixture);
        return fixture.DatabasePath;
    }
}
