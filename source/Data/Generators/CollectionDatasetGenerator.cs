using DotNetPerformanceLab.Benchmarking;
using DotNetPerformanceLab.Data.Collections;
using DotNetPerformanceLab.Models;

namespace DotNetPerformanceLab.Data.Generators;

public static class CollectionDatasetGenerator
{
    private static readonly string[] Categories = ["Apparel", "Electronics", "Home", "Beauty", "Sports", "Books", "Toys", "Grocery"];
    private static readonly decimal[] DiscountPercents = [0m, 0m, 0m, 0.05m, 0.10m, 0.15m, 0.20m];

    public static CollectionDatasetFixture Create(DatasetSize datasetSize)
    {
        CollectionDatasetProfile profile = CollectionDatasetProfiles.Get(datasetSize);
        Random random = new(BenchmarkConventions.DeterministicSeed);

        RetailTransaction[] transactions = new RetailTransaction[profile.TransactionCount];
        for (int index = 0; index < profile.TransactionCount; index++)
        {
            int productId = random.Next(1, profile.ProductCount + 1);
            string category = Categories[random.Next(Categories.Length)];
            int quantity = random.Next(1, 8);
            decimal unitPrice = Math.Round((decimal)(2 + random.NextDouble() * 998), 2, MidpointRounding.AwayFromZero);
            decimal discountPercent = DiscountPercents[random.Next(DiscountPercents.Length)];
            DateTime transactionDate = profile.BaseDateUtc
                .AddDays(random.Next(profile.DateRangeDays))
                .AddMinutes(random.Next(24 * 60));

            transactions[index] = new RetailTransaction(
                TransactionId: index + 1,
                ProductId: productId,
                Category: category,
                Quantity: quantity,
                UnitPrice: unitPrice,
                DiscountPercent: discountPercent,
                TransactionDate: DateTime.SpecifyKind(transactionDate, DateTimeKind.Utc));
        }

        return new CollectionDatasetFixture(datasetSize, transactions);
    }
}
