namespace phần3;

public sealed class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? ImagePath { get; set; }
}

public sealed class ProductCategory
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
