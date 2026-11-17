using DotNetPerformanceLab.Data.Database;
using DotNetPerformanceLab.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetPerformanceLab.Data.Generators;

public static class RetailDatabaseSeeder
{
    private static readonly string[] Regions = ["North", "South", "East", "West", "Central"];
    private static readonly string[] Categories = ["Apparel", "Electronics", "Home", "Beauty", "Sports", "Books"];
    private static readonly string[] Statuses = ["Pending", "Processing", "Shipped", "Completed", "Cancelled"];
    private static readonly decimal[] DiscountPercents = [0m, 0.05m, 0.10m, 0.15m, 0.20m];

    public static void Seed(RetailDbContext dbContext, RetailDatasetProfile profile, int seed)
    {
        Random random = new(seed);

        List<Customer> customers = new(profile.CustomerCount);
        for (int index = 1; index <= profile.CustomerCount; index++)
        {
            customers.Add(new Customer
            {
                Name = $"Customer {index:000000}",
                Email = $"customer{index:000000}@dotnetperformancelab.local",
                Region = Regions[random.Next(Regions.Length)]
            });
        }

        dbContext.Customers.AddRange(customers);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        List<Product> products = new(profile.ProductCount);
        for (int index = 1; index <= profile.ProductCount; index++)
        {
            decimal unitPrice = Math.Round((decimal)(5 + (random.NextDouble() * 495)), 2, MidpointRounding.AwayFromZero);
            products.Add(new Product
            {
                Sku = $"SKU-{index:000000}",
                Name = $"Product {index:000000}",
                Category = Categories[random.Next(Categories.Length)],
                UnitPrice = unitPrice
            });
        }

        dbContext.Products.AddRange(products);
        dbContext.SaveChanges();
        dbContext.ChangeTracker.Clear();

        int[] customerIds = dbContext.Customers.AsNoTracking().Select(x => x.Id).ToArray();
        Product[] seededProducts = dbContext.Products.AsNoTracking().ToArray();

        const int orderBatchSize = 2_000;
        int generatedOrders = 0;
        while (generatedOrders < profile.OrderCount)
        {
            int remaining = profile.OrderCount - generatedOrders;
            int batchSize = Math.Min(orderBatchSize, remaining);

            List<Order> orders = new(batchSize);
            for (int index = 0; index < batchSize; index++)
            {
                DateTime orderDateUtc = profile.BaseOrderDateUtc
                    .AddDays(random.Next(365))
                    .AddMinutes(random.Next(24 * 60));

                Order order = new()
                {
                    CustomerId = customerIds[random.Next(customerIds.Length)],
                    OrderDate = DateTime.SpecifyKind(orderDateUtc, DateTimeKind.Utc),
                    Status = Statuses[random.Next(Statuses.Length)]
                };

                int lineCount = random.Next(profile.MinLinesPerOrder, profile.MaxLinesPerOrder + 1);
                for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    Product product = seededProducts[random.Next(seededProducts.Length)];
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = random.Next(1, 6),
                        UnitPrice = product.UnitPrice,
                        DiscountPercent = DiscountPercents[random.Next(DiscountPercents.Length)]
                    });
                }

                orders.Add(order);
            }

            dbContext.Orders.AddRange(orders);
            dbContext.SaveChanges();
            dbContext.ChangeTracker.Clear();
            generatedOrders += batchSize;
        }
    }
}
