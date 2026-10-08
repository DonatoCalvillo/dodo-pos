using FerreSys.Domain;

namespace FerreSys.Application;

public static class StockMovementFactory
{
    public static string CurrentActor =>
        string.IsNullOrWhiteSpace(Environment.UserName) ? "Sistema" : Environment.UserName;

    public static StockMovement Create(
        Product product,
        StockMovementKind kind,
        decimal quantityBefore,
        decimal quantityDelta,
        decimal quantityAfter,
        Guid? referenceId = null,
        string notes = "",
        DateTime? occurredAt = null)
    {
        return new StockMovement(
            Guid.NewGuid(),
            product.Id,
            product.Barcode,
            product.Name,
            occurredAt ?? DateTime.Now,
            kind,
            quantityBefore,
            quantityDelta,
            quantityAfter,
            CurrentActor,
            referenceId,
            notes);
    }

    public static StockMovement? CreateIfChanged(
        Product product,
        StockMovementKind kind,
        decimal quantityBefore,
        decimal quantityAfter,
        Guid? referenceId = null,
        string notes = "",
        DateTime? occurredAt = null)
    {
        decimal delta = quantityAfter - quantityBefore;
        if (delta == 0m)
            return null;

        return Create(product, kind, quantityBefore, delta, quantityAfter, referenceId, notes, occurredAt);
    }
}
