namespace FerreSys.Domain;

public enum LoyaltyEarnMode
{
    PercentOfSale,
    PointsPerAmount
}

public enum LoyaltyEarnBase
{
    PaidTotal,
    Subtotal
}

public sealed class LoyaltyProgramSettings
{
    public bool Enabled { get; init; }
    public LoyaltyEarnMode EarnMode { get; init; } = LoyaltyEarnMode.PercentOfSale;
    public decimal EarnPercent { get; init; } = 10m;
    public int PointsPerBlock { get; init; } = 1;
    public decimal AmountPerBlock { get; init; } = 10m;
    public LoyaltyEarnBase EarnBase { get; init; } = LoyaltyEarnBase.PaidTotal;

    public int CalculatePoints(decimal subtotal, decimal paidTotal)
    {
        if (!Enabled)
            return 0;

        decimal basis = EarnBase == LoyaltyEarnBase.Subtotal ? subtotal : paidTotal;
        if (basis <= 0m)
            return 0;

        decimal raw = EarnMode switch
        {
            LoyaltyEarnMode.PercentOfSale => basis * EarnPercent / 100m,
            LoyaltyEarnMode.PointsPerAmount when AmountPerBlock > 0m && PointsPerBlock > 0 =>
                decimal.Floor(basis / AmountPerBlock) * PointsPerBlock,
            _ => 0m
        };

        if (raw <= 0m)
            return 0;

        if (raw >= int.MaxValue)
            return int.MaxValue;

        return (int)decimal.Floor(raw);
    }
}
