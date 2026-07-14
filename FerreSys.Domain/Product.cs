namespace FerreSys.Domain;

public class Product
{
    // Unique identifier
    public Guid Id { get; private set; }

    // Barcode (manufacturer code or internal generated code)
    public string Barcode { get; private set; }

    // Product description (e.g., "16oz Claw Hammer")
    public string Name { get; private set; }

    // Wholesaler purchase price (needed to calculate profit margins later)
    public decimal CostPrice { get; private set; }

    // Retail price at the counter
    public decimal RetailPrice { get; private set; }

    // Current stock availability in store/warehouse
    public int Stock { get; private set; }

    public Product(Guid id, string barcode, string name, decimal costPrice, decimal retailPrice, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto no puede estar vacío.");

        if (retailPrice < costPrice)
            throw new ArgumentException("El precio de venta no puede ser menor al precio de costo (perderías dinero).");

        if (stock < 0)
            throw new ArgumentException("El stock inicial no puede ser negativo.");

        Id = id;
        Barcode = barcode;
        Name = name;
        CostPrice = costPrice;
        RetailPrice = retailPrice;
        Stock = stock;
    }

    // BUSINESS RULE: Deduct stock on checkout
    public void RecordSale(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("La cantidad a vender debe ser mayor a cero.");

        if (quantity > Stock)
            throw new InvalidOperationException($"Stock insuficiente para '{Name}'. Disponible: {Stock}");

        Stock -= quantity;
    }

    // BUSINESS RULE: Restock inventory
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("La cantidad a agregar debe ser mayor a cero.");

        Stock += quantity;
    }
}