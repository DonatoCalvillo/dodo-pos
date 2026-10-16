namespace FerreSys.Domain;

public sealed class PurchaseDecisionLine
{
    public PurchaseDecisionLine(Guid lineId, decimal receivedQty)
    {
        if (lineId == Guid.Empty)
            throw new ArgumentException("La línea de recepción no es válida.");
        if (receivedQty < 0)
            throw new ArgumentException("La cantidad recibida no puede ser negativa.");

        LineId = lineId;
        ReceivedQty = receivedQty;
    }

    public Guid LineId { get; }
    public decimal ReceivedQty { get; }
}

public interface IPurchaseOrderRepository
{
    IReadOnlyList<PurchaseOrder> List(
        Guid? supplierId,
        PurchaseOrderStatus? status,
        DateTime? fromInclusive,
        DateTime? toInclusive);

    PurchaseOrder? Get(Guid id);

    PurchaseOrder SaveDraft(PurchaseOrder order);

    void MarkSent(Guid id, DateTime sentAt);

    PurchaseOrder ApplyDecision(
        Guid orderId,
        PurchaseOrderStatus status,
        DateTime decidedAt,
        string actor,
        IReadOnlyList<PurchaseDecisionLine> lines);
}
