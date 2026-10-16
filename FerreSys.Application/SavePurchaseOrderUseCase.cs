using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class PurchaseOrderLineDraft
{
    public Guid ProductId { get; init; }
    public decimal OrderedQty { get; init; }
    public decimal UnitCost { get; init; }
}

public sealed class PurchaseOrderDraft
{
    public Guid? Id { get; init; }
    public Guid SupplierId { get; init; }
    public string Notes { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public IReadOnlyList<PurchaseOrderLineDraft> Lines { get; init; } = [];
}

public sealed class SavePurchaseOrderUseCase
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly ISupplierRepository _suppliers;
    private readonly IProductRepository _products;

    public SavePurchaseOrderUseCase(
        IPurchaseOrderRepository orders,
        ISupplierRepository suppliers,
        IProductRepository products)
    {
        _orders = orders;
        _suppliers = suppliers;
        _products = products;
    }

    public PurchaseOrder Execute(PurchaseOrderDraft draft)
    {
        if (draft.SupplierId == Guid.Empty)
            throw new InvalidOperationException("Selecciona un proveedor.");

        Supplier? supplier = _suppliers.Get(draft.SupplierId);
        if (supplier == null)
            throw new InvalidOperationException("El proveedor ya no existe.");

        string notes = draft.Notes.Trim();
        if (notes.Length > 500)
            throw new InvalidOperationException("Las notas no pueden pasar de 500 caracteres.");

        if (draft.Lines.Count == 0)
            throw new InvalidOperationException("Agrega al menos un producto.");

        if (draft.Lines.Select(line => line.ProductId).Distinct().Count() != draft.Lines.Count)
            throw new InvalidOperationException("Un producto no puede repetirse en la orden.");

        PurchaseOrder? existing = draft.Id is Guid id ? _orders.Get(id) : null;
        if (draft.Id is Guid && existing == null)
            throw new InvalidOperationException("La orden ya no existe.");
        if (existing != null && !existing.IsDraft)
            throw new InvalidOperationException("La orden ya fue cerrada y no se puede editar.");
        if (!supplier.IsActive && existing?.SupplierId != supplier.Id)
            throw new InvalidOperationException("El proveedor está inactivo.");

        var lines = new List<PurchaseOrderLine>(draft.Lines.Count);
        foreach (PurchaseOrderLineDraft line in draft.Lines)
        {
            Product? product = _products.GetById(line.ProductId);
            if (product == null || !product.IsActive)
                throw new InvalidOperationException("Hay un producto que ya no está en el catálogo.");

            Product.EnsureQuantity(line.OrderedQty, product.Unit, allowZero: false);
            if (line.UnitCost < 0 || line.UnitCost > 9_999_999.99m)
                throw new InvalidOperationException($"El costo de '{product.Name}' no es válido.");

            decimal qty = decimal.Round(line.OrderedQty, product.Unit.MaxDecimalPlaces(), MidpointRounding.AwayFromZero);
            lines.Add(new PurchaseOrderLine(
                Guid.NewGuid(),
                product.Id,
                product.Barcode,
                product.Name,
                product.Unit,
                qty,
                0,
                decimal.Round(line.UnitCost, 2, MidpointRounding.AwayFromZero)));
        }

        var order = new PurchaseOrder(
            existing?.Id ?? Guid.NewGuid(),
            existing?.Number ?? "OC-00000",
            supplier.Id,
            supplier.Name,
            PurchaseOrderStatus.Draft,
            existing?.CreatedAt ?? DateTime.Now,
            existing?.SentAt,
            null,
            notes,
            existing?.CreatedBy ?? draft.CreatedBy,
            lines);

        return _orders.SaveDraft(order);
    }
}
