namespace FerreSys.Domain;

public enum PurchaseOrderStatus
{
    Draft,
    Accepted,
    Partial,
    Rejected
}

public static class PurchaseOrderStatusExtensions
{
    public static string ToCode(this PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "BORRADOR",
        PurchaseOrderStatus.Accepted => "ACEPTADA",
        PurchaseOrderStatus.Partial => "PARCIAL",
        PurchaseOrderStatus.Rejected => "RECHAZADA",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static string DisplayName(this PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Draft => "Borrador",
        PurchaseOrderStatus.Accepted => "Aceptada",
        PurchaseOrderStatus.Partial => "Parcial",
        PurchaseOrderStatus.Rejected => "Rechazada",
        _ => status.ToString()
    };

    public static PurchaseOrderStatus Parse(string? code)
    {
        return code?.Trim().ToUpperInvariant() switch
        {
            "BORRADOR" => PurchaseOrderStatus.Draft,
            "ACEPTADA" => PurchaseOrderStatus.Accepted,
            "PARCIAL" => PurchaseOrderStatus.Partial,
            "RECHAZADA" => PurchaseOrderStatus.Rejected,
            _ => throw new ArgumentException("Estado de orden de compra inválido.")
        };
    }
}
