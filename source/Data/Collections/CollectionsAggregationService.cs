using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DotNetPerformanceLab.Models;
using DotNetPerformanceLab.Reporting.Collections;

namespace DotNetPerformanceLab.Data.Collections;

public sealed class CollectionsAggregationService
{
    public SalesSummary AggregateWithLinq(IReadOnlyList<RetailTransaction> transactions)
    {
        int totalTransactionCount = transactions.Count;
        int totalUnits = transactions.Sum(transaction => transaction.Quantity);
        decimal grossSales = RoundMoney(transactions.Sum(transaction => transaction.UnitPrice * transaction.Quantity));
        decimal totalDiscounts = RoundMoney(transactions.Sum(transaction => (transaction.UnitPrice * transaction.Quantity) * transaction.DiscountPercent));
        decimal netSales = RoundMoney(grossSales - totalDiscounts);

        List<CategorySalesSummary> categorySales = transactions
            .GroupBy(transaction => transaction.Category, StringComparer.Ordinal)
            .Select(group =>
            {
                int transactionCount = group.Count();
                int units = group.Sum(x => x.Quantity);
                decimal gross = RoundMoney(group.Sum(x => x.UnitPrice * x.Quantity));
                decimal discounts = RoundMoney(group.Sum(x => (x.UnitPrice * x.Quantity) * x.DiscountPercent));
                decimal net = RoundMoney(gross - discounts);
                return new CategorySalesSummary(group.Key, transactionCount, units, gross, discounts, net);
            })
            .OrderBy(summary => summary.Category, StringComparer.Ordinal)
            .ToList();

        List<(string Category, int TransactionCount)> transactionCounts = categorySales
            .Select(summary => (summary.Category, summary.TransactionCount))
            .ToList();

        return new SalesSummary(totalTransactionCount, totalUnits, grossSales, totalDiscounts, netSales, categorySales, transactionCounts);
    }

    public SalesSummary AggregateWithForeach(IReadOnlyList<RetailTransaction> transactions)
    {
        int totalTransactionCount = 0;
        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;
        Dictionary<string, MutableCategoryAccumulator> byCategory = new(StringComparer.Ordinal);

        foreach (RetailTransaction transaction in transactions)
        {
            totalTransactionCount++;
            totalUnits += transaction.Quantity;

            decimal lineGross = transaction.UnitPrice * transaction.Quantity;
            decimal lineDiscount = lineGross * transaction.DiscountPercent;

            grossSales += lineGross;
            totalDiscounts += lineDiscount;

            if (!byCategory.TryGetValue(transaction.Category, out MutableCategoryAccumulator? accumulator))
            {
                accumulator = new MutableCategoryAccumulator(transaction.Category);
                byCategory.Add(transaction.Category, accumulator);
            }

            accumulator.TransactionCount++;
            accumulator.TotalUnits += transaction.Quantity;
            accumulator.GrossSales += lineGross;
            accumulator.TotalDiscounts += lineDiscount;
        }

        return BuildSummary(totalTransactionCount, totalUnits, grossSales, totalDiscounts, byCategory);
    }

    public SalesSummary AggregateWithPresizedDictionary(IReadOnlyList<RetailTransaction> transactions, int estimatedCategoryCount)
    {
        if (estimatedCategoryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCategoryCount), estimatedCategoryCount, "Estimated category count must be zero or greater.");
        }

        Dictionary<string, MutableCategoryAccumulator> byCategory = estimatedCategoryCount == 0
            ? new Dictionary<string, MutableCategoryAccumulator>(StringComparer.Ordinal)
            : new Dictionary<string, MutableCategoryAccumulator>(estimatedCategoryCount, StringComparer.Ordinal);

        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;

        for (int index = 0; index < transactions.Count; index++)
        {
            RetailTransaction transaction = transactions[index];
            totalUnits += transaction.Quantity;

            decimal lineGross = transaction.UnitPrice * transaction.Quantity;
            decimal lineDiscount = lineGross * transaction.DiscountPercent;

            grossSales += lineGross;
            totalDiscounts += lineDiscount;

            if (!byCategory.TryGetValue(transaction.Category, out MutableCategoryAccumulator? accumulator))
            {
                accumulator = new MutableCategoryAccumulator(transaction.Category);
                byCategory.Add(transaction.Category, accumulator);
            }

            accumulator.TransactionCount++;
            accumulator.TotalUnits += transaction.Quantity;
            accumulator.GrossSales += lineGross;
            accumulator.TotalDiscounts += lineDiscount;
        }

        return BuildSummary(transactions.Count, totalUnits, grossSales, totalDiscounts, byCategory);
    }

    public SalesSummary AggregateWithCollectionsMarshal(IReadOnlyList<RetailTransaction> transactions, int estimatedCategoryCount)
    {
        if (estimatedCategoryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedCategoryCount), estimatedCategoryCount, "Estimated category count must be zero or greater.");
        }

        Dictionary<string, MutableCategoryAccumulator> byCategory = estimatedCategoryCount == 0
            ? new Dictionary<string, MutableCategoryAccumulator>(StringComparer.Ordinal)
            : new Dictionary<string, MutableCategoryAccumulator>(estimatedCategoryCount, StringComparer.Ordinal);

        int totalUnits = 0;
        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;

        foreach (RetailTransaction transaction in transactions)
        {
            totalUnits += transaction.Quantity;

            decimal lineGross = transaction.UnitPrice * transaction.Quantity;
            decimal lineDiscount = lineGross * transaction.DiscountPercent;

            grossSales += lineGross;
            totalDiscounts += lineDiscount;

            ref MutableCategoryAccumulator? accumulatorRef = ref CollectionsMarshal.GetValueRefOrAddDefault(byCategory, transaction.Category, out bool exists);
            if (!exists || accumulatorRef is null)
            {
                accumulatorRef = new MutableCategoryAccumulator(transaction.Category);
            }

            MutableCategoryAccumulator accumulator = accumulatorRef ?? throw new InvalidOperationException("Category accumulator should always be initialized.");
            accumulator.TransactionCount++;
            accumulator.TotalUnits += transaction.Quantity;
            accumulator.GrossSales += lineGross;
            accumulator.TotalDiscounts += lineDiscount;
        }

        return BuildSummary(transactions.Count, totalUnits, grossSales, totalDiscounts, byCategory);
    }

    public SalesSummary AggregateWithParallelFor(IReadOnlyList<RetailTransaction> transactions)
    {
        decimal grossSales = 0m;
        decimal totalDiscounts = 0m;
        int totalUnits = 0;
        object totalLock = new();
        ConcurrentBag<Dictionary<string, MutableCategoryAccumulator>> localResults = [];

        Parallel.For(
            fromInclusive: 0,
            toExclusive: transactions.Count,
            localInit: () => new ThreadLocalAggregate(),
            body: (index, _, local) =>
            {
                RetailTransaction transaction = transactions[index];
                decimal lineGross = transaction.UnitPrice * transaction.Quantity;
                decimal lineDiscount = lineGross * transaction.DiscountPercent;

                local.TotalUnits += transaction.Quantity;
                local.GrossSales += lineGross;
                local.TotalDiscounts += lineDiscount;

                if (!local.ByCategory.TryGetValue(transaction.Category, out MutableCategoryAccumulator? accumulator))
                {
                    accumulator = new MutableCategoryAccumulator(transaction.Category);
                    local.ByCategory.Add(transaction.Category, accumulator);
                }

                accumulator.TransactionCount++;
                accumulator.TotalUnits += transaction.Quantity;
                accumulator.GrossSales += lineGross;
                accumulator.TotalDiscounts += lineDiscount;
                return local;
            },
            localFinally: local =>
            {
                lock (totalLock)
                {
                    totalUnits += local.TotalUnits;
                    grossSales += local.GrossSales;
                    totalDiscounts += local.TotalDiscounts;
                }

                localResults.Add(local.ByCategory);
            });

        Dictionary<string, MutableCategoryAccumulator> merged = new(StringComparer.Ordinal);
        foreach (Dictionary<string, MutableCategoryAccumulator> partial in localResults)
        {
            foreach ((string category, MutableCategoryAccumulator value) in partial)
            {
                if (!merged.TryGetValue(category, out MutableCategoryAccumulator? accumulator))
                {
                    accumulator = new MutableCategoryAccumulator(category);
                    merged.Add(category, accumulator);
                }

                accumulator.TransactionCount += value.TransactionCount;
                accumulator.TotalUnits += value.TotalUnits;
                accumulator.GrossSales += value.GrossSales;
                accumulator.TotalDiscounts += value.TotalDiscounts;
            }
        }

        return BuildSummary(transactions.Count, totalUnits, grossSales, totalDiscounts, merged);
    }

    public SalesSummary AggregateWithPlinq(IReadOnlyList<RetailTransaction> transactions)
    {
        ThreadLocalAggregate aggregate = transactions
            .AsParallel()
            .Aggregate(
                seedFactory: () => new ThreadLocalAggregate(),
                updateAccumulatorFunc: (local, transaction) =>
                {
                    decimal lineGross = transaction.UnitPrice * transaction.Quantity;
                    decimal lineDiscount = lineGross * transaction.DiscountPercent;

                    local.TotalUnits += transaction.Quantity;
                    local.GrossSales += lineGross;
                    local.TotalDiscounts += lineDiscount;

                    if (!local.ByCategory.TryGetValue(transaction.Category, out MutableCategoryAccumulator? accumulator))
                    {
                        accumulator = new MutableCategoryAccumulator(transaction.Category);
                        local.ByCategory.Add(transaction.Category, accumulator);
                    }

                    accumulator.TransactionCount++;
                    accumulator.TotalUnits += transaction.Quantity;
                    accumulator.GrossSales += lineGross;
                    accumulator.TotalDiscounts += lineDiscount;
                    return local;
                },
                combineAccumulatorsFunc: (left, right) =>
                {
                    left.TotalUnits += right.TotalUnits;
                    left.GrossSales += right.GrossSales;
                    left.TotalDiscounts += right.TotalDiscounts;

                    foreach ((string category, MutableCategoryAccumulator value) in right.ByCategory)
                    {
                        if (!left.ByCategory.TryGetValue(category, out MutableCategoryAccumulator? accumulator))
                        {
                            accumulator = new MutableCategoryAccumulator(category);
                            left.ByCategory.Add(category, accumulator);
                        }

                        accumulator.TransactionCount += value.TransactionCount;
                        accumulator.TotalUnits += value.TotalUnits;
                        accumulator.GrossSales += value.GrossSales;
                        accumulator.TotalDiscounts += value.TotalDiscounts;
                    }

                    return left;
                },
                resultSelector: local => local);

        return BuildSummary(transactions.Count, aggregate.TotalUnits, aggregate.GrossSales, aggregate.TotalDiscounts, aggregate.ByCategory);
    }

    public SalesSummary Aggregate(IReadOnlyList<RetailTransaction> transactions, CollectionsAggregationApproach approach) => approach switch
    {
        CollectionsAggregationApproach.Linq => AggregateWithLinq(transactions),
        CollectionsAggregationApproach.Foreach => AggregateWithForeach(transactions),
        CollectionsAggregationApproach.PresizedDictionary => AggregateWithPresizedDictionary(transactions, estimatedCategoryCount: 0),
        CollectionsAggregationApproach.CollectionsMarshal => AggregateWithCollectionsMarshal(transactions, estimatedCategoryCount: 0),
        CollectionsAggregationApproach.ParallelFor => AggregateWithParallelFor(transactions),
        CollectionsAggregationApproach.Plinq => AggregateWithPlinq(transactions),
        _ => throw new ArgumentOutOfRangeException(nameof(approach), approach, "Unsupported collections aggregation approach.")
    };

    private static SalesSummary BuildSummary(
        int transactionCount,
        int totalUnits,
        decimal grossSales,
        decimal totalDiscounts,
        IReadOnlyDictionary<string, MutableCategoryAccumulator> byCategory)
    {
        decimal roundedGrossSales = RoundMoney(grossSales);
        decimal roundedDiscounts = RoundMoney(totalDiscounts);
        decimal netSales = RoundMoney(roundedGrossSales - roundedDiscounts);

        List<CategorySalesSummary> categorySales = byCategory
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
            {
                MutableCategoryAccumulator value = pair.Value;
                decimal categoryGross = RoundMoney(value.GrossSales);
                decimal categoryDiscounts = RoundMoney(value.TotalDiscounts);
                decimal categoryNet = RoundMoney(categoryGross - categoryDiscounts);

                return new CategorySalesSummary(
                    pair.Key,
                    value.TransactionCount,
                    value.TotalUnits,
                    categoryGross,
                    categoryDiscounts,
                    categoryNet);
            })
            .ToList();

        List<(string Category, int TransactionCount)> categoryTransactionCounts = categorySales
            .Select(summary => (summary.Category, summary.TransactionCount))
            .ToList();

        return new SalesSummary(
            transactionCount,
            totalUnits,
            roundedGrossSales,
            roundedDiscounts,
            netSales,
            categorySales,
            categoryTransactionCounts);
    }

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed class ThreadLocalAggregate
    {
        public int TotalUnits { get; set; }

        public decimal GrossSales { get; set; }

        public decimal TotalDiscounts { get; set; }

        public Dictionary<string, MutableCategoryAccumulator> ByCategory { get; } = new(StringComparer.Ordinal);
    }

    private sealed class MutableCategoryAccumulator
    {
        public MutableCategoryAccumulator(string category)
        {
            Category = category;
        }

        public string Category { get; }

        public int TransactionCount { get; set; }

        public int TotalUnits { get; set; }

        public decimal GrossSales { get; set; }

        public decimal TotalDiscounts { get; set; }
    }
}
