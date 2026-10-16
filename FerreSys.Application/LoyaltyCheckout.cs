using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class LoyaltyCheckoutRequest
{
    public Guid CustomerId { get; init; }
    public Guid? RewardId { get; init; }
    public decimal LoyaltyDiscountAmount { get; init; }
}

public static class LoyaltyReturnPlanner
{
    public static IReadOnlyList<LoyaltyMovement> Plan(
        Sale sale,
        decimal returnedAmount,
        bool returnsAllRemaining,
        LoyaltyReversalTotals already)
    {
        LoyaltySaleInfo? loyalty = sale.Loyalty;
        if (loyalty == null)
            return [];

        int remainingEarn = Math.Max(0, loyalty.PointsEarned - already.ReversedEarn);
        int remainingRedeem = Math.Max(0, loyalty.PointsRedeemed - already.RestoredRedeem);

        int earnReverse;
        int redeemRestore;
        if (returnsAllRemaining)
        {
            earnReverse = remainingEarn;
            redeemRestore = remainingRedeem;
        }
        else
        {
            int raw = sale.Total <= 0m
                ? 0
                : (int)decimal.Floor(loyalty.PointsEarned * returnedAmount / sale.Total);
            earnReverse = Math.Min(Math.Max(raw, 0), remainingEarn);
            redeemRestore = 0;
        }

        var movements = new List<LoyaltyMovement>();
        DateTime now = DateTime.Now;
        if (earnReverse > 0)
        {
            movements.Add(new LoyaltyMovement(
                Guid.NewGuid(),
                loyalty.CustomerId,
                sale.Id,
                now,
                LoyaltyMovementKind.Reversal,
                -earnReverse,
                null,
                "Reverso de puntos por devolución"));
        }

        if (redeemRestore > 0)
        {
            movements.Add(new LoyaltyMovement(
                Guid.NewGuid(),
                loyalty.CustomerId,
                sale.Id,
                now,
                LoyaltyMovementKind.Reversal,
                redeemRestore,
                loyalty.RewardId,
                "Devolución de puntos del premio"));
        }

        return movements;
    }
}
