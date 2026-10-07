namespace FerreSys.Infrastructure.UI;

internal sealed class CartLineSnapshot
{
    public required string Barcode { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public decimal Subtotal { get; init; }
}

internal sealed class CartSession
{
    public const int MaxCartSessions = 5;

    public Guid Id { get; } = Guid.NewGuid();
    public required string Label { get; set; }
    public List<CartLineSnapshot> Lines { get; } = [];
}
