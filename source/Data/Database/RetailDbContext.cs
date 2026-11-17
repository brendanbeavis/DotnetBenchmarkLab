using DotNetPerformanceLab.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DotNetPerformanceLab.Data.Database;

public sealed class RetailDbContext : DbContext
{
    private static readonly ValueConverter<decimal, double> DecimalToDoubleConverter = new(
        value => Convert.ToDouble(value),
        value => Convert.ToDecimal(value));

    public RetailDbContext(DbContextOptions<RetailDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Region).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Region);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UnitPrice)
                .HasConversion(DecimalToDoubleConverter)
                .IsRequired();
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.HasIndex(x => x.Category);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.Property(x => x.OrderDate).IsRequired();

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OrderDate);
            entity.HasIndex(x => new { x.OrderDate, x.CustomerId });
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UnitPrice)
                .HasConversion(DecimalToDoubleConverter)
                .IsRequired();

            entity.Property(x => x.DiscountPercent)
                .HasConversion(DecimalToDoubleConverter)
                .IsRequired();

            entity.Property(x => x.Quantity).IsRequired();

            entity.HasOne(x => x.Order)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Product)
                .WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => new { x.OrderId, x.ProductId });
        });
    }
}
