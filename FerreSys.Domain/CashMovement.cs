namespace FerreSys.Domain;

public enum CashMovementKind
{
    Entrada,
    Salida
}

public class CashMovement
{
    public Guid Id { get; }
    public DateTime BusinessDate { get; }
    public CashMovementKind Kind { get; }
    public decimal Amount { get; }
    public string Concept { get; }
    public DateTime CreatedAt { get; }

    public CashMovement(
        Guid id,
        DateTime businessDate,
        CashMovementKind kind,
        decimal amount,
        string concept,
        DateTime createdAt)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");

        string trimmed = (concept ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("El concepto es obligatorio.");

        Id = id;
        BusinessDate = businessDate.Date;
        Kind = kind;
        Amount = amount;
        Concept = trimmed;
        CreatedAt = createdAt;
    }

    public string KindCode => Kind == CashMovementKind.Entrada ? "ENTRADA" : "SALIDA";

    public static CashMovementKind ParseKind(string kind)
    {
        return kind?.Trim().ToUpperInvariant() switch
        {
            "ENTRADA" => CashMovementKind.Entrada,
            "SALIDA" => CashMovementKind.Salida,
            _ => throw new ArgumentException("Tipo de movimiento inválido.")
        };
    }
}

public class CashMovementDayTotals
{
    public decimal Entries { get; }
    public decimal Exits { get; }

    public CashMovementDayTotals(decimal entries, decimal exits)
    {
        Entries = entries;
        Exits = exits;
    }

    public decimal Net => Entries - Exits;
}
