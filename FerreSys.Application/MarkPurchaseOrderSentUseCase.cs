using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class MarkPurchaseOrderSentUseCase
{
    private readonly IPurchaseOrderRepository _orders;

    public MarkPurchaseOrderSentUseCase(IPurchaseOrderRepository orders)
    {
        _orders = orders;
    }

    public void Execute(Guid orderId, DateTime sentAt)
    {
        _orders.MarkSent(orderId, sentAt);
    }
}
