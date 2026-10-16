namespace FerreSys.Domain;

public sealed class LoyaltySaleInfo
{
    public Guid CustomerId { get; }
    public string CustomerName { get; }
    public int PointsEarned { get; }
    public Guid? RewardId { get; }
    public string RewardName { get; }
    public int PointsRedeemed { get; }
    public decimal DiscountAmount { get; }
    public int BalanceAfter { get; }

    public LoyaltySaleInfo(
        Guid customerId,
        string customerName,
        int pointsEarned,
        Guid? rewardId,
        string rewardName,
        int pointsRedeemed,
        decimal discountAmount,
        int balanceAfter)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("La venta de fidelidad necesita un cliente.");
        if (pointsEarned < 0 || pointsRedeemed < 0)
            throw new ArgumentException("Los puntos de la venta no pueden ser negativos.");
        if (discountAmount < 0m)
            throw new ArgumentException("El descuento del premio no puede ser negativo.");
        if (balanceAfter < 0)
            throw new ArgumentException("El saldo de puntos no puede quedar negativo.");

        CustomerId = customerId;
        CustomerName = customerName?.Trim() ?? string.Empty;
        PointsEarned = pointsEarned;
        RewardId = rewardId;
        RewardName = rewardName?.Trim() ?? string.Empty;
        PointsRedeemed = pointsRedeemed;
        DiscountAmount = decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
        BalanceAfter = balanceAfter;
    }
}
