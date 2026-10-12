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

    // Reorder threshold; 0 disables below-minimum alerts
    public decimal MinStock { get; private set; }

    // Target / maximum stock used for purchase suggestions
    public decimal DesiredStock { get; private set; }

    // Logical delete: hidden from the counter, kept for past sales
    public bool IsActive { get; private set; }

    public Product(
        Guid id,
        string barcode,
        string name,
        decimal costPrice,
        decimal retailPrice,
        decimal stock,
        UnitOfMeasure unit = UnitOfMeasure.Pieza,
        decimal minStock = 0m,
        decimal desiredStock = 0m,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto no puede estar vacío.");

        if (retailPrice < costPrice)
            throw new ArgumentException("El precio de venta no puede ser menor al precio de costo (perderías dinero).");

        if (stock < 0)
            throw new ArgumentException("El stock inicial no puede ser negativo.");

        if (minStock < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.");

        if (desiredStock < 0)
            throw new ArgumentException("El stock deseado no puede ser negativo.");

        if (minStock > 0m && desiredStock > 0m && desiredStock < minStock)
            throw new ArgumentException("El stock deseado no puede ser menor al stock mínimo.");

        ValidateQuantityForUnit(stock, unit, allowZero: true);
        ValidateQuantityForUnit(minStock, unit, allowZero: true);
        ValidateQuantityForUnit(desiredStock, unit, allowZero: true);

        Id = id;
        Barcode = barcode;
        Name = name;
        CostPrice = costPrice;
        RetailPrice = retailPrice;
        Stock = stock;
        Unit = unit;
        MinStock = minStock;
        DesiredStock = desiredStock;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;

    public bool IsOutOfStock => Stock <= 0m;

    public bool IsBelowMinimum => MinStock > 0m && Stock <= MinStock;

    public decimal SuggestedOrderQty => Math.Max(0m, DesiredStock - Stock);

    public StockAlertLevel AlertLevel =>
        IsOutOfStock
            ? StockAlertLevel.Critical
            : IsBelowMinimum
                ? StockAlertLevel.Warning
                : StockAlertLevel.None;

    public bool NeedsReorder => IsOutOfStock || IsBelowMinimum;

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

    /// <summary>
    /// Sets absolute stock (audit / edit). Returns (before, delta, after).
    /// </summary>
    public (decimal Before, decimal Delta, decimal After) AdjustStock(decimal newStock)
    {
        if (newStock < 0)
            throw new ArgumentException("El stock de inventario no puede ser una cantidad negativa.");

        ValidateQuantityForUnit(newStock, Unit, allowZero: true);

        decimal before = Stock;
        decimal after = decimal.Round(newStock, Unit.MaxDecimalPlaces(), MidpointRounding.AwayFromZero);
        Stock = after;
        return (before, after - before, after);
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
