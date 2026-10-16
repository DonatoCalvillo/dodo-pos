namespace FerreSys.Domain;

public sealed class PurchaseOrderLine
{
    public PurchaseOrderLine(
        Guid id,
        Guid productId,
        string barcode,
        string name,
        UnitOfMeasure unit,
        decimal orderedQty,
        decimal receivedQty,
        decimal unitCost)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("La línea necesita un identificador.");
        if (productId == Guid.Empty)
            throw new ArgumentException("La línea necesita un producto.");
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("El código del producto es obligatorio.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto es obligatorio.");
        if (orderedQty <= 0)
            throw new ArgumentException("La cantidad pedida debe ser mayor a cero.");
        if (receivedQty < 0)
            throw new ArgumentException("La cantidad recibida no puede ser negativa.");
        if (unitCost < 0)
            throw new ArgumentException("El costo no puede ser negativo.");

        Id = id;
        ProductId = productId;
        Barcode = barcode.Trim();
        Name = name.Trim();
        Unit = unit;
        OrderedQty = orderedQty;
        ReceivedQty = receivedQty;
        UnitCost = decimal.Round(unitCost, 2, MidpointRounding.AwayFromZero);
    }

    public Guid Id { get; }
    public Guid ProductId { get; }
    public string Barcode { get; }
    public string Name { get; }
    public UnitOfMeasure Unit { get; }
    public decimal OrderedQty { get; }
    public decimal ReceivedQty { get; }
    public decimal UnitCost { get; }

    public decimal OrderedAmount =>
        decimal.Round(OrderedQty * UnitCost, 2, MidpointRounding.AwayFromZero);

    public decimal ReceivedAmount =>
        decimal.Round(ReceivedQty * UnitCost, 2, MidpointRounding.AwayFromZero);
}

public sealed class PurchaseOrder
{
    public PurchaseOrder(
        Guid id,
        string number,
        Guid supplierId,
        string supplierName,
        PurchaseOrderStatus status,
        DateTime createdAt,
        DateTime? sentAt,
        DateTime? decidedAt,
        string notes,
        string createdBy,
        IReadOnlyList<PurchaseOrderLine> lines)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("La orden necesita un identificador.");
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("El folio de la orden es obligatorio.");
        if (supplierId == Guid.Empty)
            throw new ArgumentException("La orden necesita un proveedor.");
        if (lines == null || lines.Count == 0)
            throw new ArgumentException("La orden necesita al menos un producto.");

        Id = id;
        Number = number.Trim();
        SupplierId = supplierId;
        SupplierName = supplierName?.Trim() ?? string.Empty;
        Status = status;
        CreatedAt = createdAt;
        SentAt = sentAt;
        DecidedAt = decidedAt;
        Notes = notes?.Trim() ?? string.Empty;
        CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "Sistema" : createdBy.Trim();
        Lines = lines;
    }

    public Guid Id { get; }
    public string Number { get; }
    public Guid SupplierId { get; }
    public string SupplierName { get; }
    public PurchaseOrderStatus Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime? SentAt { get; }
    public DateTime? DecidedAt { get; }
    public string Notes { get; }
    public string CreatedBy { get; }
    public IReadOnlyList<PurchaseOrderLine> Lines { get; }

    public bool IsDraft => Status == PurchaseOrderStatus.Draft;

    public decimal Total =>
        decimal.Round(Lines.Sum(line => line.OrderedAmount), 2, MidpointRounding.AwayFromZero);
}
