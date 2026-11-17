namespace DotNetPerformanceLab.Models;

public sealed class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = [];
}
