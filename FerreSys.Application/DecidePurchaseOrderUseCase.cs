using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class PurchaseReceiptDraft
{
    public Guid LineId { get; init; }
    public decimal ReceivedQty { get; init; }
}

public sealed class DecidePurchaseOrderUseCase
{
    private readonly IPurchaseOrderRepository _orders;

    public DecidePurchaseOrderUseCase(IPurchaseOrderRepository orders)
    {
        _orders = orders;
    }

    public PurchaseOrder AcceptAll(Guid orderId, string actor)
    {
        PurchaseOrder order = RequireDraft(orderId);
        var lines = order.Lines
            .Select(line => new PurchaseDecisionLine(line.Id, line.OrderedQty))
            .ToList();
        return _orders.ApplyDecision(order.Id, PurchaseOrderStatus.Accepted, DateTime.Now, actor, lines);
    }

    public PurchaseOrder Reject(Guid orderId, string actor)
    {
        PurchaseOrder order = RequireDraft(orderId);
        var lines = order.Lines
            .Select(line => new PurchaseDecisionLine(line.Id, 0))
            .ToList();
        return _orders.ApplyDecision(order.Id, PurchaseOrderStatus.Rejected, DateTime.Now, actor, lines);
    }

    public PurchaseOrder Receive(Guid orderId, IReadOnlyList<PurchaseReceiptDraft> receipts, string actor)
    {
        PurchaseOrder order = RequireDraft(orderId);
        if (receipts.Count != order.Lines.Count)
            throw new InvalidOperationException("La recepción no coincide con las líneas de la orden.");

        var byId = receipts.ToDictionary(line => line.LineId);
        var lines = new List<PurchaseDecisionLine>(order.Lines.Count);
        bool allFull = true;
        bool anyReceived = false;

        foreach (PurchaseOrderLine line in order.Lines)
        {
            if (!byId.TryGetValue(line.Id, out PurchaseReceiptDraft? receipt))
                throw new InvalidOperationException("Falta una línea en la recepción.");

            Product.EnsureQuantity(receipt.ReceivedQty, line.Unit, allowZero: true);
            decimal received = decimal.Round(receipt.ReceivedQty, line.Unit.MaxDecimalPlaces(), MidpointRounding.AwayFromZero);
            if (received > line.OrderedQty)
                throw new InvalidOperationException($"No puedes recibir más de lo pedido en '{line.Name}'.");

            if (received > 0)
                anyReceived = true;
            if (received != line.OrderedQty)
                allFull = false;

            lines.Add(new PurchaseDecisionLine(line.Id, received));
        }

        if (!anyReceived)
            throw new InvalidOperationException("No hay mercancía recibida. Rechaza la orden si el proveedor no surtió.");

        PurchaseOrderStatus status = allFull
            ? PurchaseOrderStatus.Accepted
            : PurchaseOrderStatus.Partial;

        return _orders.ApplyDecision(order.Id, status, DateTime.Now, actor, lines);
    }

    private PurchaseOrder RequireDraft(Guid orderId)
    {
        PurchaseOrder? order = _orders.Get(orderId);
        if (order == null)
            throw new InvalidOperationException("La orden ya no existe.");
        if (!order.IsDraft)
            throw new InvalidOperationException("La orden ya fue cerrada.");
        return order;
    }
}
