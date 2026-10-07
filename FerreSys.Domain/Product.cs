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

    // Retail price at the counter (price per 1 unit of measure)
    public decimal RetailPrice { get; private set; }

    // Current stock availability in store/warehouse
    public decimal Stock { get; private set; }

    // Selling / inventory unit of measure
    public UnitOfMeasure Unit { get; private set; }

    public Product(
        Guid id,
        string barcode,
        string name,
        decimal costPrice,
        decimal retailPrice,
        decimal stock,
        UnitOfMeasure unit = UnitOfMeasure.Pieza)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto no puede estar vacío.");

        if (retailPrice < costPrice)
            throw new ArgumentException("El precio de venta no puede ser menor al precio de costo (perderías dinero).");

        if (stock < 0)
            throw new ArgumentException("El stock inicial no puede ser negativo.");

        ValidateQuantityForUnit(stock, unit, allowZero: true);

        Id = id;
        Barcode = barcode;
        Name = name;
        CostPrice = costPrice;
        RetailPrice = retailPrice;
        Stock = stock;
        Unit = unit;
    }

    // BUSINESS RULE: Deduct stock on checkout
    public void RecordSale(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("La cantidad a vender debe ser mayor a cero.");

        ValidateQuantityForUnit(quantity, Unit, allowZero: false);

        if (quantity > Stock)
            throw new InvalidOperationException(
                $"Stock insuficiente para '{Name}'. Disponible: {Unit.FormatQuantity(Stock)}");

        Stock -= quantity;
    }

    // BUSINESS RULE: Restock inventory
    public void AddStock(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("La cantidad a agregar debe ser mayor a cero.");

        ValidateQuantityForUnit(quantity, Unit, allowZero: false);

        Stock += quantity;
    }

    private static void ValidateQuantityForUnit(decimal quantity, UnitOfMeasure unit, bool allowZero)
    {
        if (!allowZero && quantity <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.");

        if (quantity < 0)
            throw new ArgumentException("La cantidad no puede ser negativa.");

        decimal normalized = decimal.Round(quantity, unit.MaxDecimalPlaces(), MidpointRounding.AwayFromZero);

        if (!unit.AllowsDecimals() && normalized != decimal.Truncate(normalized))
            throw new ArgumentException(
                $"La unidad '{unit.DisplayName()}' solo admite cantidades enteras.");

        if (Math.Abs(quantity - normalized) > 0.0000001m)
            throw new ArgumentException(
                $"La unidad '{unit.DisplayName()}' admite máximo {unit.MaxDecimalPlaces()} decimales.");
    }
}
