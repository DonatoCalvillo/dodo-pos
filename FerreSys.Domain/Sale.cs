namespace FerreSys.Domain;

public class SaleItem
{
    public Guid ProductId { get; private set; }
    public string Barcode { get; private set; }
    public string ProductName { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal CostPrice { get; private set; }
    public UnitOfMeasure Unit { get; private set; }
    public decimal PriceSold => UnitPrice;
    public decimal Subtotal => Quantity * UnitPrice;

    public SaleItem(
        Guid productId,
        string barcode,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal costPrice,
        UnitOfMeasure unit = UnitOfMeasure.Pieza)
    {
        ProductId = productId;
        Barcode = barcode;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        CostPrice = costPrice;
        Unit = unit;
    }
}

public class Sale
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public List<SaleItem> Items { get; private set; }
    public decimal Subtotal => Items.Sum(x => x.Subtotal);
    public decimal DiscountAmount { get; private set; }
    public decimal Total => Subtotal - DiscountAmount;
    public string PaymentMethod { get; private set; } // "EFECTIVO", "TARJETA" o "MIXTO"
    public string CardReference { get; private set; } // Últimos 4 dígitos o folio (opcional)
    public decimal AmountReceived { get; private set; }
    public decimal CardAmount { get; private set; }
    public decimal CashAmount => Total - CardAmount;
    public decimal Change => CashAmount > 0 ? AmountReceived - CashAmount : 0m;

    public Sale(
        Guid id,
        DateTime createdAt,
        List<SaleItem> items,
        string paymentMethod,
        string cardReference,
        decimal amountReceived = 0,
        decimal cardAmount = 0,
        decimal discountAmount = 0)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Una venta debe contener al menos un producto.");

        if (paymentMethod != "EFECTIVO" && paymentMethod != "TARJETA" && paymentMethod != "MIXTO")
            throw new ArgumentException("Método de pago inválido.");

        Id = id;
        CreatedAt = createdAt;
        Items = items;
        PaymentMethod = paymentMethod;
        CardReference = cardReference ?? string.Empty;
        DiscountAmount = decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero);

        if (DiscountAmount < 0m)
            throw new ArgumentException("El descuento de la venta no puede ser negativo.");

        if (DiscountAmount > 0m && DiscountAmount >= Subtotal)
            throw new ArgumentException("El descuento de la venta debe ser menor al subtotal.");

        decimal total = Total;
        decimal roundedCard = decimal.Round(cardAmount, 2, MidpointRounding.AwayFromZero);
        decimal roundedReceived = amountReceived > 0
            ? decimal.Round(amountReceived, 2, MidpointRounding.AwayFromZero)
            : 0m;

        if (paymentMethod == "EFECTIVO")
        {
            if (roundedCard != 0m)
                throw new ArgumentException("Un pago en efectivo no puede incluir monto de tarjeta.");

            CardAmount = 0m;
            decimal received = roundedReceived > 0 ? roundedReceived : total;
            if (received < total)
                throw new ArgumentException("El efectivo recibido no cubre el total de la venta.");

            AmountReceived = received;
        }
        else if (paymentMethod == "TARJETA")
        {
            // Ventas legacy: CardAmount en BD puede ser 0; se interpreta como Total.
            CardAmount = roundedCard > 0 ? roundedCard : total;
            if (CardAmount != total)
                throw new ArgumentException("El monto de tarjeta debe cubrir el total de la venta.");

            AmountReceived = 0m;
        }
        else // MIXTO
        {
            if (roundedCard <= 0m || roundedCard >= total)
                throw new ArgumentException("En cobro mixto, el monto de tarjeta debe ser mayor a 0 y menor al total.");

            if (string.IsNullOrWhiteSpace(CardReference))
                throw new ArgumentException("El cobro mixto requiere referencia de tarjeta.");

            CardAmount = roundedCard;
            decimal cashNeeded = total - CardAmount;
            decimal received = roundedReceived > 0 ? roundedReceived : cashNeeded;
            if (received < cashNeeded)
                throw new ArgumentException("El efectivo recibido no cubre la parte en efectivo de la venta.");

            AmountReceived = received;
        }
    }
}
