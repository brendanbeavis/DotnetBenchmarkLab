using DotNetPerformanceLab.Reporting.Database;

namespace DotNetPerformanceLab.Data.Database;

public sealed class DatabaseCorrectnessVerifier
{
    private readonly DatabaseReportingQueryService _queryService;

    public DatabaseCorrectnessVerifier(DatabaseReportingQueryService queryService)
    {
        _queryService = queryService;
    }

    public DatabaseCorrectnessVerificationResult VerifyAllEquivalent(DatabaseReportQuery query, decimal decimalTolerance = 0.01m)
    {
        Dictionary<DatabaseReportingApproach, IReadOnlyList<DatabaseOrderReportDto>> resultsByApproach = new()
        {
            [DatabaseReportingApproach.EfTracking] = _queryService.RunEfTracking(query),
            [DatabaseReportingApproach.EfNoTracking] = _queryService.RunEfNoTracking(query),
            [DatabaseReportingApproach.EfProjection] = _queryService.RunEfProjection(query),
            [DatabaseReportingApproach.EfCompiledProjection] = _queryService.RunEfCompiledProjection(query),
            [DatabaseReportingApproach.Dapper] = _queryService.RunDapper(query),
            [DatabaseReportingApproach.AdoNet] = _queryService.RunAdoNet(query)
        };

        IReadOnlyList<DatabaseOrderReportDto> baseline = resultsByApproach[DatabaseReportingApproach.EfNoTracking];
        foreach ((DatabaseReportingApproach approach, IReadOnlyList<DatabaseOrderReportDto> candidate) in resultsByApproach)
        {
            AssertEquivalent(baseline, candidate, approach, decimalTolerance);
        }

        return new DatabaseCorrectnessVerificationResult(resultsByApproach, decimalTolerance);
    }

    private static void AssertEquivalent(
        IReadOnlyList<DatabaseOrderReportDto> baseline,
        IReadOnlyList<DatabaseOrderReportDto> candidate,
        DatabaseReportingApproach approach,
        decimal decimalTolerance)
    {
        if (baseline.Count != candidate.Count)
        {
            throw new InvalidOperationException($"Result count mismatch for {approach}: baseline={baseline.Count}, candidate={candidate.Count}.");
        }

        for (int index = 0; index < baseline.Count; index++)
        {
            DatabaseOrderReportDto expected = baseline[index];
            DatabaseOrderReportDto actual = candidate[index];

            if (expected.OrderId != actual.OrderId)
            {
                throw new InvalidOperationException($"OrderId mismatch at index {index} for {approach}: expected={expected.OrderId}, actual={actual.OrderId}.");
            }

            if (expected.OrderDate != actual.OrderDate)
            {
                throw new InvalidOperationException($"OrderDate mismatch for OrderId={expected.OrderId}, approach={approach}.");
            }

            if (!string.Equals(expected.Status, actual.Status, StringComparison.Ordinal)
                || expected.CustomerId != actual.CustomerId
                || !string.Equals(expected.CustomerName, actual.CustomerName, StringComparison.Ordinal)
                || !string.Equals(expected.CustomerEmail, actual.CustomerEmail, StringComparison.Ordinal)
                || !string.Equals(expected.Region, actual.Region, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Order header mismatch for OrderId={expected.OrderId}, approach={approach}.");
            }

            if (expected.TotalQuantity != actual.TotalQuantity)
            {
                throw new InvalidOperationException($"TotalQuantity mismatch for OrderId={expected.OrderId}, approach={approach}: expected={expected.TotalQuantity}, actual={actual.TotalQuantity}.");
            }

            if (Math.Abs(expected.OrderTotal - actual.OrderTotal) > decimalTolerance)
            {
                throw new InvalidOperationException($"OrderTotal mismatch for OrderId={expected.OrderId}, approach={approach}: expected={expected.OrderTotal}, actual={actual.OrderTotal}.");
            }

            if (expected.Lines.Count != actual.Lines.Count)
            {
                throw new InvalidOperationException($"Line count mismatch for OrderId={expected.OrderId}, approach={approach}: expected={expected.Lines.Count}, actual={actual.Lines.Count}.");
            }

            for (int lineIndex = 0; lineIndex < expected.Lines.Count; lineIndex++)
            {
                DatabaseOrderLineReportDto expectedLine = expected.Lines[lineIndex];
                DatabaseOrderLineReportDto actualLine = actual.Lines[lineIndex];

                if (expectedLine.ProductId != actualLine.ProductId
                    || !string.Equals(expectedLine.Sku, actualLine.Sku, StringComparison.Ordinal)
                    || !string.Equals(expectedLine.ProductName, actualLine.ProductName, StringComparison.Ordinal)
                    || !string.Equals(expectedLine.Category, actualLine.Category, StringComparison.Ordinal)
                    || expectedLine.Quantity != actualLine.Quantity)
                {
                    throw new InvalidOperationException($"Line identity mismatch for OrderId={expected.OrderId}, lineIndex={lineIndex}, approach={approach}.");
                }

                if (Math.Abs(expectedLine.UnitPrice - actualLine.UnitPrice) > decimalTolerance
                    || Math.Abs(expectedLine.DiscountPercent - actualLine.DiscountPercent) > decimalTolerance
                    || Math.Abs(expectedLine.LineTotal - actualLine.LineTotal) > decimalTolerance)
                {
                    throw new InvalidOperationException($"Line decimal mismatch for OrderId={expected.OrderId}, lineIndex={lineIndex}, approach={approach}.");
                }
            }
        }
    }
}
