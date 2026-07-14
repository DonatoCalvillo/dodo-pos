namespace FerreSys.Domain;

public class SaleItem
{
    public Guid ProductId { get; private set; }
    public string Barcode { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal PriceSold { get; private set; }
    public decimal Subtotal => Quantity * PriceSold;

    public SaleItem(Guid productId, string barcode, string productName, int quantity, decimal priceSold)
    {
        ProductId = productId;
        Barcode = barcode;
        ProductName = productName;
        Quantity = quantity;
        PriceSold = priceSold;
    }
}

public class Sale
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<SaleItem> Items { get; private set; }
    public decimal Total => Items.Sum(x => x.Subtotal);

    public Sale(Guid id, DateTime createdAt, List<SaleItem> items)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Una venta debe contener al menos un producto.");

        Id = id;
        CreatedAt = createdAt;
        Items = items;
    }
}