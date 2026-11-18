using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Json;
using DotNetPerformanceLab.Models.Json;

namespace DotNetPerformanceLab.Data.Generators;

public static class JsonPayloadFixtureGenerator
{
    private static readonly string[] Regions = ["North", "South", "East", "West", "Central"];
    private static readonly string[] Statuses = ["Pending", "Processing", "Shipped", "Completed", "Cancelled"];
    private static readonly string[] Categories = ["Apparel", "Electronics", "Home", "Beauty", "Sports", "Books"];
    private static readonly decimal[] DiscountPercents = [0m, 0m, 0.05m, 0.10m, 0.15m, 0.20m];

    private static readonly JsonSerializerOptions StableSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonPayloadFixture Create(DatasetSize datasetSize)
    {
        JsonDatasetProfile profile = JsonDatasetProfiles.Get(datasetSize);
        Random random = new(BenchmarkConventions.DeterministicSeed);

        ApiCustomerDto[] customers = GenerateCustomers(profile.CustomerCount, random);

        ApiOrdersPayloadDto payload = new()
        {
            Metadata = new ApiPayloadMetadataDto
            {
                RequestId = $"req-{datasetSize.ToString().ToLowerInvariant()}-{BenchmarkConventions.DeterministicSeed}",
                GeneratedAtUtc = profile.BaseDateUtc,
                SourceSystem = "RetailApiGateway"
            },
            Orders = GenerateOrders(profile, customers, random)
        };

        int totalLineItemCount = payload.Orders.Sum(order => order.LineItems.Count);

        string payloadText = JsonSerializer.Serialize(payload, StableSerializationOptions);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadText);

        return new JsonPayloadFixture(datasetSize, payloadBytes, payloadText, totalLineItemCount);
    }

    private static ApiCustomerDto[] GenerateCustomers(int customerCount, Random random)
    {
        ApiCustomerDto[] customers = new ApiCustomerDto[customerCount];
        for (int index = 0; index < customerCount; index++)
        {
            int customerId = index + 1;
            customers[index] = new ApiCustomerDto
            {
                CustomerId = customerId,
                Name = $"Customer {customerId:000000}",
                Email = $"customer{customerId:000000}@dotnetperformancelab.local",
                Region = Regions[random.Next(Regions.Length)]
            };
        }

        return customers;
    }

    private static List<ApiOrderDto> GenerateOrders(JsonDatasetProfile profile, ApiCustomerDto[] customers, Random random)
    {
        List<ApiOrderDto> orders = new(profile.OrderCount);

        for (int index = 0; index < profile.OrderCount; index++)
        {
            int orderId = index + 1;
            ApiCustomerDto customerTemplate = customers[random.Next(customers.Length)];

            DateTime orderDateUtc = profile.BaseDateUtc
                .AddDays(random.Next(365))
                .AddMinutes(random.Next(24 * 60));

            int lineItemCount = random.Next(profile.MinLineItemsPerOrder, profile.MaxLineItemsPerOrder + 1);

            List<ApiOrderLineItemDto> lineItems = new(lineItemCount);
            for (int lineIndex = 0; lineIndex < lineItemCount; lineIndex++)
            {
                int productId = random.Next(1, profile.ProductCount + 1);
                decimal unitPrice = Math.Round((decimal)(2 + random.NextDouble() * 998), 2, MidpointRounding.AwayFromZero);
                decimal discountPercent = DiscountPercents[random.Next(DiscountPercents.Length)];

                lineItems.Add(new ApiOrderLineItemDto
                {
                    ProductId = productId,
                    Sku = $"SKU-{productId:000000}",
                    Description = $"{Categories[random.Next(Categories.Length)]} Product {productId:000000}",
                    Quantity = random.Next(1, 8),
                    UnitPrice = unitPrice,
                    DiscountPercent = discountPercent,
                    Notes = lineIndex % 5 == 0 ? null : "standard"
                });
            }

            orders.Add(new ApiOrderDto
            {
                OrderId = orderId,
                Status = Statuses[random.Next(Statuses.Length)],
                OrderDateUtc = DateTime.SpecifyKind(orderDateUtc, DateTimeKind.Utc),
                Customer = new ApiCustomerDto
                {
                    CustomerId = customerTemplate.CustomerId,
                    Name = customerTemplate.Name,
                    Email = customerTemplate.Email,
                    Region = customerTemplate.Region
                },
                LineItems = lineItems
            });
        }

        return orders;
    }
}
