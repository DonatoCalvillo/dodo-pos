namespace FerreSys.Domain;

public enum LoyaltyRewardKind
{
    Percent,
    FixedAmount,
    Benefit
}

public sealed class LoyaltyReward
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int PointsCost { get; }
    public LoyaltyRewardKind Kind { get; }
    public decimal Value { get; }
    public bool IsActive { get; }

    public LoyaltyReward(
        Guid id,
        string name,
        string description,
        int pointsCost,
        LoyaltyRewardKind kind,
        decimal value,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("El premio necesita un identificador.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del premio es obligatorio.");
        if (pointsCost <= 0)
            throw new ArgumentException("El premio debe costar al menos 1 punto.");
        if (!Enum.IsDefined(kind))
            throw new ArgumentException("El tipo de premio no es válido.");

        decimal rounded = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        if (kind == LoyaltyRewardKind.Benefit)
            rounded = 0m;
        else if (rounded <= 0m)
            throw new ArgumentException("El valor del descuento debe ser mayor a cero.");
        else if (kind == LoyaltyRewardKind.Percent && rounded >= 100m)
            throw new ArgumentException("El descuento en porcentaje debe ser menor a 100.");

        Id = id;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        PointsCost = pointsCost;
        Kind = kind;
        Value = rounded;
        IsActive = isActive;
    }

    public decimal DiscountFor(decimal subtotal)
    {
        if (subtotal <= 0m)
            return 0m;

        decimal raw = Kind switch
        {
            LoyaltyRewardKind.Percent => decimal.Round(subtotal * Value / 100m, 2, MidpointRounding.AwayFromZero),
            LoyaltyRewardKind.FixedAmount => Value,
            _ => 0m
        };

        return raw < 0m ? 0m : raw;
    }

    public string ValueLabel => Kind switch
    {
        LoyaltyRewardKind.Percent => $"{Value:0.##}%",
        LoyaltyRewardKind.FixedAmount => $"${Value:N2}",
        _ => string.IsNullOrWhiteSpace(Description) ? "Beneficio" : Description
    };
}
