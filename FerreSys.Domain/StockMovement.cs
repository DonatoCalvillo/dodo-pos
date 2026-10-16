namespace FerreSys.Domain;

public enum StockMovementKind
{
    Venta,
    AjusteAuditoria,
    CargaInicial,
    Devolucion,
    Compra
}

public class StockMovement
{
    public Guid Id { get; }
    public Guid ProductId { get; }
    public string Barcode { get; }
    public string ProductName { get; }
    public DateTime OccurredAt { get; }
    public StockMovementKind Kind { get; }
    public decimal QuantityBefore { get; }
    public decimal QuantityDelta { get; }
    public decimal QuantityAfter { get; }
    public string Actor { get; }
    public Guid? ReferenceId { get; }
    public string Notes { get; }

    public StockMovement(
        Guid id,
        Guid productId,
        string barcode,
        string productName,
        DateTime occurredAt,
        StockMovementKind kind,
        decimal quantityBefore,
        decimal quantityDelta,
        decimal quantityAfter,
        string actor,
        Guid? referenceId = null,
        string notes = "")
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("El código de barras es obligatorio.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("El nombre del producto es obligatorio.");

        if (Math.Abs((quantityBefore + quantityDelta) - quantityAfter) > 0.0000001m)
            throw new ArgumentException("El stock resultante no coincide con anterior + movimiento.");

        Id = id;
        ProductId = productId;
        Barcode = barcode.Trim();
        ProductName = productName.Trim();
        OccurredAt = occurredAt;
        Kind = kind;
        QuantityBefore = quantityBefore;
        QuantityDelta = quantityDelta;
        QuantityAfter = quantityAfter;
        Actor = string.IsNullOrWhiteSpace(actor) ? "Sistema" : actor.Trim();
        ReferenceId = referenceId;
        Notes = (notes ?? string.Empty).Trim();
    }

    public string KindCode => ToKindCode(Kind);

    public static string ToKindCode(StockMovementKind kind) => kind switch
    {
        StockMovementKind.Venta => "VENTA",
        StockMovementKind.AjusteAuditoria => "AJUSTE_AUDITORIA",
        StockMovementKind.CargaInicial => "CARGA_INICIAL",
        StockMovementKind.Devolucion => "DEVOLUCION",
        StockMovementKind.Compra => "COMPRA",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public string KindDisplayName => Kind switch
    {
        StockMovementKind.Venta => "Venta",
        StockMovementKind.AjusteAuditoria => "Ajuste de Auditoría",
        StockMovementKind.CargaInicial => "Carga Inicial",
        StockMovementKind.Devolucion => "Devolución",
        StockMovementKind.Compra => "Compra",
        _ => Kind.ToString()
    };

    public static StockMovementKind ParseKind(string kind)
    {
        return kind?.Trim().ToUpperInvariant() switch
        {
            "VENTA" => StockMovementKind.Venta,
            "AJUSTE_AUDITORIA" => StockMovementKind.AjusteAuditoria,
            "CARGA_INICIAL" => StockMovementKind.CargaInicial,
            "DEVOLUCION" => StockMovementKind.Devolucion,
            "COMPRA" => StockMovementKind.Compra,
            _ => throw new ArgumentException("Tipo de movimiento de inventario inválido.")
        };
    }
}
