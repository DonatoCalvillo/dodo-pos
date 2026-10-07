namespace FerreSys.Application;

public sealed class ChangeBreakdown
{
    public decimal TotalChange { get; }
    public IReadOnlyList<(decimal Denomination, int Count)> Bills { get; }
    public decimal Remainder { get; }

    public ChangeBreakdown(decimal totalChange, IReadOnlyList<(decimal Denomination, int Count)> bills, decimal remainder)
    {
        TotalChange = totalChange;
        Bills = bills;
        Remainder = remainder;
    }
}

public static class ChangeCalculator
{
    private static readonly decimal[] CommonBills = [1000m, 500m, 200m, 100m];

    public static ChangeBreakdown BreakDown(decimal change)
    {
        change = decimal.Round(Math.Max(0m, change), 2, MidpointRounding.AwayFromZero);
        var bills = new List<(decimal Denomination, int Count)>();
        decimal remaining = change;

        foreach (decimal bill in CommonBills)
        {
            int count = (int)(remaining / bill);
            if (count > 0)
            {
                bills.Add((bill, count));
                remaining -= count * bill;
            }
        }

        remaining = decimal.Round(remaining, 2, MidpointRounding.AwayFromZero);
        return new ChangeBreakdown(change, bills, remaining);
    }

    public static IEnumerable<string> FormatLines(decimal change)
    {
        var breakdown = BreakDown(change);
        foreach (var (denomination, count) in breakdown.Bills)
            yield return $"{count} × ${denomination:0}";

        if (breakdown.Remainder > 0m)
            yield return $"Resto: ${breakdown.Remainder:N2}";
    }
}
