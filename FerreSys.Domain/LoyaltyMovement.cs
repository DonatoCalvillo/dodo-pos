namespace FerreSys.Domain;

public enum LoyaltyMovementKind
{
    Earn,
    Redeem,
    Reversal
}

public sealed class LoyaltyMovement
{
    public Guid Id { get; }
    public Guid CustomerId { get; }
    public Guid? SaleId { get; }
    public DateTime OccurredAt { get; }
    public LoyaltyMovementKind Kind { get; }
    public int Points { get; }
    public Guid? RewardId { get; }
    public string Note { get; }

    public LoyaltyMovement(
        Guid id,
        Guid customerId,
        Guid? saleId,
        DateTime occurredAt,
        LoyaltyMovementKind kind,
        int points,
        Guid? rewardId,
        string note)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El movimiento necesita un identificador.");
        if (customerId == Guid.Empty)
            throw new ArgumentException("El movimiento necesita un cliente.");
        if (points == 0)
            throw new ArgumentException("Un movimiento de puntos no puede ser cero.");
        if (!Enum.IsDefined(kind))
            throw new ArgumentException("El tipo de movimiento no es válido.");

        Id = id;
        CustomerId = customerId;
        SaleId = saleId;
        OccurredAt = occurredAt;
        Kind = kind;
        Points = points;
        RewardId = rewardId;
        Note = note?.Trim() ?? string.Empty;
    }
}

public readonly record struct LoyaltyReversalTotals(int ReversedEarn, int RestoredRedeem);
