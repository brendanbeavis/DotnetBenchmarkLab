namespace DotNetPerformanceLab.Models;

public sealed record RetailTransaction(
    int TransactionId,
    int ProductId,
    string Category,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    DateTime TransactionDate);
