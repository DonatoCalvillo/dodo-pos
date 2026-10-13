namespace FerreSys.Domain;

public class SaleReturnLine
{
    public Guid ProductId { get; }
    public string Barcode { get; }
    public string ProductName { get; }
    public decimal Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal CostPrice { get; }
    public decimal LineAmount { get; }

    public SaleReturnLine(
        Guid productId,
        string barcode,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal costPrice,
        decimal lineAmount)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("El producto de la devolución es obligatorio.");

        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("El código de barras es obligatorio.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("El nombre del producto es obligatorio.");

        if (quantity <= 0m)
            throw new ArgumentException("La cantidad devuelta debe ser mayor a cero.");

        if (unitPrice < 0m || costPrice < 0m || lineAmount < 0m)
            throw new ArgumentException("Los importes de la devolución no pueden ser negativos.");

        ProductId = productId;
        Barcode = barcode.Trim();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        CostPrice = costPrice;
        LineAmount = decimal.Round(lineAmount, 2, MidpointRounding.AwayFromZero);
    }
}

public class SaleReturn
{
    public Guid Id { get; }
    public Guid SaleId { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<SaleReturnLine> Lines { get; }
    public decimal Amount { get; }
    public bool RestoreStock { get; }
    public string Notes { get; }

    public SaleReturn(
        Guid id,
        Guid saleId,
        DateTime createdAt,
        IReadOnlyList<SaleReturnLine> lines,
        bool restoreStock,
        string notes = "")
    {
        if (id == Guid.Empty)
            throw new ArgumentException("La devolución necesita un identificador.");

        if (saleId == Guid.Empty)
            throw new ArgumentException("La devolución debe estar ligada a una venta.");

        if (lines == null || lines.Count == 0)
            throw new ArgumentException("La devolución debe incluir al menos un producto.");

        Id = id;
        SaleId = saleId;
        CreatedAt = createdAt;
        Lines = lines;
        Amount = decimal.Round(lines.Sum(line => line.LineAmount), 2, MidpointRounding.AwayFromZero);
        RestoreStock = restoreStock;
        Notes = (notes ?? string.Empty).Trim();

        if (Amount <= 0m)
            throw new ArgumentException("El monto a devolver debe ser mayor a cero.");
    }
}

public class SaleReturnTotals
{
    public decimal Amount { get; }
    public IReadOnlyDictionary<Guid, decimal> QuantityByProductId { get; }

    public SaleReturnTotals(decimal amount, IReadOnlyDictionary<Guid, decimal> quantityByProductId)
    {
        Amount = amount;
        QuantityByProductId = quantityByProductId;
    }

    public decimal QuantityFor(Guid productId) =>
        QuantityByProductId.TryGetValue(productId, out decimal quantity) ? quantity : 0m;
}
