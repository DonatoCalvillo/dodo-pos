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

    // Optional JPEG thumbnail. Null when the product has no photo.
    public byte[]? Image { get; private set; }

    // None, percent off, or a fixed amount off the unit price.
    public DiscountKind DiscountKind { get; private set; }

    // Percent (0–100) or money off, depending on DiscountKind.
    public decimal DiscountValue { get; private set; }

    // Inclusive start. Null means the discount can apply immediately.
    public DateOnly? DiscountStartsOn { get; private set; }

    // Inclusive end. Null means the discount has no end date.
    public DateOnly? DiscountEndsOn { get; private set; }

    // Unit price once the cart reaches WholesaleMinQuantity. Null disables wholesale.
    public decimal? WholesalePrice { get; private set; }

    public decimal? WholesaleMinQuantity { get; private set; }

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
        bool isActive = true,
        byte[]? image = null,
        DiscountKind discountKind = DiscountKind.None,
        decimal discountValue = 0m,
        DateOnly? discountStartsOn = null,
        DateOnly? discountEndsOn = null,
        decimal? wholesalePrice = null,
        decimal? wholesaleMinQuantity = null)
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
        ValidateSpecialPricing(
            costPrice,
            retailPrice,
            unit,
            ref discountKind,
            ref discountValue,
            ref discountStartsOn,
            ref discountEndsOn,
            ref wholesalePrice,
            ref wholesaleMinQuantity);

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
        Image = image is { Length: > 0 } ? image.ToArray() : null;
        DiscountKind = discountKind;
        DiscountValue = discountValue;
        DiscountStartsOn = discountStartsOn;
        DiscountEndsOn = discountEndsOn;
        WholesalePrice = wholesalePrice;
        WholesaleMinQuantity = wholesaleMinQuantity;
    }

    public bool IsDiscountActive(DateOnly on)
    {
        if (DiscountKind == DiscountKind.None || DiscountValue <= 0m)
            return false;

        if (DiscountStartsOn is DateOnly start && on < start)
            return false;

        if (DiscountEndsOn is DateOnly end && on > end)
            return false;

        return true;
    }

    public decimal ResolveUnitPrice(decimal quantity, DateOnly on)
    {
        decimal basePrice = RetailPrice;
        if (WholesalePrice is decimal wholesale
            && WholesaleMinQuantity is decimal minQty
            && quantity >= minQty)
        {
            basePrice = wholesale;
        }

        if (!IsDiscountActive(on))
            return RoundMoney(basePrice);

        decimal priced = DiscountKind switch
        {
            DiscountKind.Percent => basePrice * (1m - DiscountValue / 100m),
            DiscountKind.FixedAmount => basePrice - DiscountValue,
            _ => basePrice
        };

        return RoundMoney(priced);
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

    private static void ValidateSpecialPricing(
        decimal costPrice,
        decimal retailPrice,
        UnitOfMeasure unit,
        ref DiscountKind discountKind,
        ref decimal discountValue,
        ref DateOnly? discountStartsOn,
        ref DateOnly? discountEndsOn,
        ref decimal? wholesalePrice,
        ref decimal? wholesaleMinQuantity)
    {
        if (discountKind == DiscountKind.None)
        {
            discountValue = 0m;
            discountStartsOn = null;
            discountEndsOn = null;
        }
        else
        {
            discountValue = RoundMoney(discountValue);
            if (discountKind == DiscountKind.Percent)
            {
                if (discountValue <= 0m || discountValue >= 100m)
                    throw new ArgumentException("El porcentaje de descuento debe ser mayor a 0 y menor a 100.");
            }
            else if (discountValue <= 0m)
            {
                throw new ArgumentException("El monto de descuento debe ser mayor a cero.");
            }

            if (discountStartsOn is DateOnly start && discountEndsOn is DateOnly end && end < start)
                throw new ArgumentException("La fecha final del descuento no puede ser anterior a la inicial.");
        }

        bool hasWholesalePrice = wholesalePrice.HasValue;
        bool hasWholesaleQty = wholesaleMinQuantity.HasValue;
        if (hasWholesalePrice != hasWholesaleQty)
            throw new ArgumentException("Indica el precio de mayoreo y a partir de cuántas unidades aplica.");

        if (wholesalePrice is decimal wholesale && wholesaleMinQuantity is decimal minQty)
        {
            wholesale = RoundMoney(wholesale);
            if (wholesale <= 0m)
                throw new ArgumentException("El precio de mayoreo debe ser mayor a cero.");

            if (wholesale < costPrice)
                throw new ArgumentException("El precio de mayoreo no puede ser menor al precio de costo.");

            if (wholesale >= retailPrice)
                throw new ArgumentException("El precio de mayoreo debe ser menor al precio de venta.");

            if (minQty <= 0m)
                throw new ArgumentException("La cantidad de mayoreo debe ser mayor a cero.");

            ValidateQuantityForUnit(minQty, unit, allowZero: false);
            minQty = decimal.Round(minQty, unit.MaxDecimalPlaces(), MidpointRounding.AwayFromZero);
            wholesalePrice = wholesale;
            wholesaleMinQuantity = minQty;
        }

        EnsureDiscountKeepsMargin(costPrice, retailPrice, discountKind, discountValue, "venta");
        if (wholesalePrice is decimal wholesaleAfter)
            EnsureDiscountKeepsMargin(costPrice, wholesaleAfter, discountKind, discountValue, "mayoreo");
    }

    private static void EnsureDiscountKeepsMargin(
        decimal costPrice,
        decimal basePrice,
        DiscountKind discountKind,
        decimal discountValue,
        string label)
    {
        if (discountKind == DiscountKind.None)
            return;

        decimal priced = discountKind == DiscountKind.Percent
            ? basePrice * (1m - discountValue / 100m)
            : basePrice - discountValue;
        priced = RoundMoney(priced);

        if (priced <= 0m || priced < costPrice)
            throw new ArgumentException(
                $"El descuento deja el precio de {label} por debajo del costo.");
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static void EnsureQuantity(decimal quantity, UnitOfMeasure unit, bool allowZero) =>
        ValidateQuantityForUnit(quantity, unit, allowZero);

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
