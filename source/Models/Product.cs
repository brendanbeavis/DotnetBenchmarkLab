namespace DotNetPerformanceLab.Models;

public sealed class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
