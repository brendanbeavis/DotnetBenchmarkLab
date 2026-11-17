using System.Data;
using Dapper;
using DotNetPerformanceLab.Reporting.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceLab.Data.Database;

public sealed class DatabaseReportingQueryService
{
    private const string ProjectionSql = """
        SELECT
            o.Id AS OrderId,
            o.OrderDate AS OrderDate,
            o.Status AS Status,
            c.Id AS CustomerId,
            c.Name AS CustomerName,
            c.Email AS CustomerEmail,
            c.Region AS Region,
            p.Id AS ProductId,
            p.Sku AS Sku,
            p.Name AS ProductName,
            p.Category AS Category,
            oi.Quantity AS Quantity,
            oi.UnitPrice AS UnitPrice,
            oi.DiscountPercent AS DiscountPercent
        FROM Orders o
        INNER JOIN Customers c ON c.Id = o.CustomerId
        INNER JOIN OrderItems oi ON oi.OrderId = o.Id
        INNER JOIN Products p ON p.Id = oi.ProductId
        WHERE o.OrderDate >= @StartDateUtcInclusive AND o.OrderDate < @EndDateUtcExclusive
        ORDER BY o.OrderDate, o.Id, p.Id, oi.Id;
        """;

    private static readonly Func<RetailDbContext, DateTime, DateTime, IEnumerable<ReportRow>> CompiledProjectionQuery =
        EF.CompileQuery((RetailDbContext dbContext, DateTime startDateUtcInclusive, DateTime endDateUtcExclusive) =>
            dbContext.OrderItems
                .AsNoTracking()
                .Where(item => item.Order.OrderDate >= startDateUtcInclusive && item.Order.OrderDate < endDateUtcExclusive)
                .OrderBy(item => item.Order.OrderDate)
                .ThenBy(item => item.OrderId)
                .ThenBy(item => item.ProductId)
                .ThenBy(item => item.Id)
                .Select(item => new ReportRow
                {
                    OrderId = item.OrderId,
                    OrderDate = item.Order.OrderDate,
                    Status = item.Order.Status,
                    CustomerId = item.Order.CustomerId,
                    CustomerName = item.Order.Customer.Name,
                    CustomerEmail = item.Order.Customer.Email,
                    Region = item.Order.Customer.Region,
                    ProductId = item.ProductId,
                    Sku = item.Product.Sku,
                    ProductName = item.Product.Name,
                    Category = item.Product.Category,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent
                }));

    private readonly RetailDatabaseFixture _fixture;

    public DatabaseReportingQueryService(RetailDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public IReadOnlyList<DatabaseOrderReportDto> RunEfTracking(DatabaseReportQuery query)
    {
        using RetailDbContext dbContext = _fixture.CreateDbContext();

        List<Models.Order> orders = dbContext.Orders
            .Include(x => x.Customer)
            .Include(x => x.OrderItems)
            .ThenInclude(x => x.Product)
            .Where(x => x.OrderDate >= query.StartDateUtcInclusive && x.OrderDate < query.EndDateUtcExclusive)
            .OrderBy(x => x.OrderDate)
            .ThenBy(x => x.Id)
            .ToList();

        return orders.Select(MapTrackedOrder).ToArray();
    }

    public IReadOnlyList<DatabaseOrderReportDto> RunEfNoTracking(DatabaseReportQuery query)
    {
        using RetailDbContext dbContext = _fixture.CreateDbContext();

        List<Models.Order> orders = dbContext.Orders
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.OrderItems)
            .ThenInclude(x => x.Product)
            .Where(x => x.OrderDate >= query.StartDateUtcInclusive && x.OrderDate < query.EndDateUtcExclusive)
            .OrderBy(x => x.OrderDate)
            .ThenBy(x => x.Id)
            .ToList();

        return orders.Select(MapTrackedOrder).ToArray();
    }

    public IReadOnlyList<DatabaseOrderReportDto> RunEfProjection(DatabaseReportQuery query)
    {
        using RetailDbContext dbContext = _fixture.CreateDbContext();

        List<ReportRow> rows = dbContext.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.OrderDate >= query.StartDateUtcInclusive && item.Order.OrderDate < query.EndDateUtcExclusive)
            .OrderBy(item => item.Order.OrderDate)
            .ThenBy(item => item.OrderId)
            .ThenBy(item => item.ProductId)
            .ThenBy(item => item.Id)
            .Select(item => new ReportRow
            {
                OrderId = item.OrderId,
                OrderDate = item.Order.OrderDate,
                Status = item.Order.Status,
                CustomerId = item.Order.CustomerId,
                CustomerName = item.Order.Customer.Name,
                CustomerEmail = item.Order.Customer.Email,
                Region = item.Order.Customer.Region,
                ProductId = item.ProductId,
                Sku = item.Product.Sku,
                ProductName = item.Product.Name,
                Category = item.Product.Category,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent
            })
            .ToList();

        return BuildReport(rows);
    }

    public IReadOnlyList<DatabaseOrderReportDto> RunEfCompiledProjection(DatabaseReportQuery query)
    {
        using RetailDbContext dbContext = _fixture.CreateDbContext();
        List<ReportRow> rows = CompiledProjectionQuery(dbContext, query.StartDateUtcInclusive, query.EndDateUtcExclusive).ToList();
        return BuildReport(rows);
    }

    public IReadOnlyList<DatabaseOrderReportDto> RunDapper(DatabaseReportQuery query)
    {
        using SqliteConnection connection = new(_fixture.ConnectionString);
        connection.Open();

        List<ReportRow> rows = connection.Query<ReportRow>(
            ProjectionSql,
            new
            {
                query.StartDateUtcInclusive,
                query.EndDateUtcExclusive
            },
            commandType: CommandType.Text).ToList();

        return BuildReport(rows);
    }

    public IReadOnlyList<DatabaseOrderReportDto> RunAdoNet(DatabaseReportQuery query)
    {
        using SqliteConnection connection = new(_fixture.ConnectionString);
        connection.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = ProjectionSql;
        command.Parameters.AddWithValue("@StartDateUtcInclusive", query.StartDateUtcInclusive);
        command.Parameters.AddWithValue("@EndDateUtcExclusive", query.EndDateUtcExclusive);

        using SqliteDataReader reader = command.ExecuteReader();
        List<ReportRow> rows = [];

        int orderIdOrdinal = reader.GetOrdinal("OrderId");
        int orderDateOrdinal = reader.GetOrdinal("OrderDate");
        int statusOrdinal = reader.GetOrdinal("Status");
        int customerIdOrdinal = reader.GetOrdinal("CustomerId");
        int customerNameOrdinal = reader.GetOrdinal("CustomerName");
        int customerEmailOrdinal = reader.GetOrdinal("CustomerEmail");
        int regionOrdinal = reader.GetOrdinal("Region");
        int productIdOrdinal = reader.GetOrdinal("ProductId");
        int skuOrdinal = reader.GetOrdinal("Sku");
        int productNameOrdinal = reader.GetOrdinal("ProductName");
        int categoryOrdinal = reader.GetOrdinal("Category");
        int quantityOrdinal = reader.GetOrdinal("Quantity");
        int unitPriceOrdinal = reader.GetOrdinal("UnitPrice");
        int discountPercentOrdinal = reader.GetOrdinal("DiscountPercent");

        while (reader.Read())
        {
            rows.Add(new ReportRow
            {
                OrderId = reader.GetInt32(orderIdOrdinal),
                OrderDate = reader.GetDateTime(orderDateOrdinal),
                Status = reader.GetString(statusOrdinal),
                CustomerId = reader.GetInt32(customerIdOrdinal),
                CustomerName = reader.GetString(customerNameOrdinal),
                CustomerEmail = reader.GetString(customerEmailOrdinal),
                Region = reader.GetString(regionOrdinal),
                ProductId = reader.GetInt32(productIdOrdinal),
                Sku = reader.GetString(skuOrdinal),
                ProductName = reader.GetString(productNameOrdinal),
                Category = reader.GetString(categoryOrdinal),
                Quantity = reader.GetInt32(quantityOrdinal),
                UnitPrice = reader.GetDecimal(unitPriceOrdinal),
                DiscountPercent = reader.GetDecimal(discountPercentOrdinal)
            });
        }

        return BuildReport(rows);
    }

    private static DatabaseOrderReportDto MapTrackedOrder(Models.Order order)
    {
        List<DatabaseOrderLineReportDto> lines = order.OrderItems
            .OrderBy(item => item.ProductId)
            .ThenBy(item => item.Id)
            .Select(item =>
            {
                decimal lineTotal = CalculateLineTotal(item.Quantity, item.UnitPrice, item.DiscountPercent);
                return new DatabaseOrderLineReportDto(
                    item.ProductId,
                    item.Product.Sku,
                    item.Product.Name,
                    item.Product.Category,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountPercent,
                    lineTotal);
            })
            .ToList();

        return CreateOrderReport(
            order.Id,
            order.OrderDate,
            order.Status,
            order.CustomerId,
            order.Customer.Name,
            order.Customer.Email,
            order.Customer.Region,
            lines);
    }

    private static IReadOnlyList<DatabaseOrderReportDto> BuildReport(IEnumerable<ReportRow> rows)
    {
        return rows
            .GroupBy(row => new
            {
                row.OrderId,
                row.OrderDate,
                row.Status,
                row.CustomerId,
                row.CustomerName,
                row.CustomerEmail,
                row.Region
            })
            .OrderBy(group => group.Key.OrderDate)
            .ThenBy(group => group.Key.OrderId)
            .Select(group =>
            {
                List<DatabaseOrderLineReportDto> lines = group
                    .OrderBy(row => row.ProductId)
                    .ThenBy(row => row.Sku)
                    .Select(row =>
                    {
                        decimal lineTotal = CalculateLineTotal(row.Quantity, row.UnitPrice, row.DiscountPercent);
                        return new DatabaseOrderLineReportDto(
                            row.ProductId,
                            row.Sku,
                            row.ProductName,
                            row.Category,
                            row.Quantity,
                            row.UnitPrice,
                            row.DiscountPercent,
                            lineTotal);
                    })
                    .ToList();

                return CreateOrderReport(
                    group.Key.OrderId,
                    group.Key.OrderDate,
                    group.Key.Status,
                    group.Key.CustomerId,
                    group.Key.CustomerName,
                    group.Key.CustomerEmail,
                    group.Key.Region,
                    lines);
            })
            .ToArray();
    }

    private static DatabaseOrderReportDto CreateOrderReport(
        int orderId,
        DateTime orderDate,
        string status,
        int customerId,
        string customerName,
        string customerEmail,
        string region,
        IReadOnlyList<DatabaseOrderLineReportDto> lines)
    {
        int totalQuantity = lines.Sum(line => line.Quantity);
        decimal orderTotal = Math.Round(lines.Sum(line => line.LineTotal), 2, MidpointRounding.AwayFromZero);

        return new DatabaseOrderReportDto(
            orderId,
            DateTime.SpecifyKind(orderDate, DateTimeKind.Utc),
            status,
            customerId,
            customerName,
            customerEmail,
            region,
            totalQuantity,
            orderTotal,
            lines);
    }

    private static decimal CalculateLineTotal(int quantity, decimal unitPrice, decimal discountPercent)
    {
        decimal gross = quantity * unitPrice;
        decimal discount = gross * discountPercent;
        decimal net = gross - discount;
        return Math.Round(net, 2, MidpointRounding.AwayFromZero);
    }

    private sealed class ReportRow
    {
        public int OrderId { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public string Sku { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }
    }
}
